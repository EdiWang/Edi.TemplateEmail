using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.DependencyInjection;
using System.Xml.Serialization;
using Xunit;

namespace Edi.TemplateEmail.Tests;

public class ServiceCollectionExtensionsTests : IDisposable
{
    private readonly string _testConfigPath = Path.GetTempFileName();

    public ServiceCollectionExtensionsTests()
    {
        File.WriteAllText(_testConfigPath, """
            <MailConfiguration>
              <MailMessage MessageType="Welcome" IsHtml="false">
                <MessageSubject>Welcome {Name.Value}</MessageSubject>
                <MessageBody>Hello {Name.Value}</MessageBody>
              </MailMessage>
            </MailConfiguration>
            """);
    }

    public void Dispose()
    {
        File.Delete(_testConfigPath);
    }

    [Fact]
    public void AddTemplateEmail_WithConfigPath_ShouldRegisterIsolatedTransientHelpersAndSingletonSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var smtpSettings = new SmtpSettings("smtp.example.com", null, null, 587);
        var configureCalls = 0;

        // Act
        var result = services.AddTemplateEmail(_testConfigPath, settings =>
        {
            configureCalls++;
            settings.FromAddress = "sender@example.com";
            settings.SmtpSettings = smtpSettings;
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var first = scope.ServiceProvider.GetRequiredService<IEmailHelper>();
        var second = scope.ServiceProvider.GetRequiredService<IEmailHelper>();
        IEmailBuilder aliceBuilder = first.ForType("Welcome").Map("Name", "Alice");
        var bobBuilder = first.ForType("Welcome").Map("Name", "Bob");
        var otherBuilder = second.ForType("Welcome").Map("Name", "Other");
        var bobMessage = bobBuilder.BuildMessage(["bob@example.com"]);
        var aliceMessage = aliceBuilder.BuildMessage(["alice@example.com"]);
        var otherMessage = otherBuilder.BuildMessage(["other@example.com"]);
        var settings = scope.ServiceProvider.GetRequiredService<EmailSettings>();

        // Assert
        Assert.Same(services, result);
        Assert.NotSame(first, second);
        Assert.Equal("Welcome Alice", aliceMessage.Subject);
        Assert.Equal("Hello Alice", aliceMessage.Body);
        Assert.Equal("Welcome Bob", bobMessage.Subject);
        Assert.Equal("Hello Bob", bobMessage.Body);
        Assert.Equal("Welcome Other", otherMessage.Subject);
        Assert.Equal("sender@example.com", settings.FromAddress);
        Assert.Same(smtpSettings, settings.SmtpSettings);
        Assert.Same(settings, provider.GetRequiredService<EmailSettings>());
        Assert.Equal(1, configureCalls);
    }

    [Fact]
    public void AddTemplateEmail_WithConfigPath_ShouldReadChangesOnlyForNewHelpers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTemplateEmail(_testConfigPath, _ => { });
        using var provider = services.BuildServiceProvider();
        var originalHelper = provider.GetRequiredService<IEmailHelper>();
        var originalBuilder = originalHelper.ForType("Welcome").Map("Name", "Alice");

        // Act
        File.WriteAllText(_testConfigPath, File.ReadAllText(_testConfigPath)
            .Replace("Welcome {Name.Value}", "Updated {Name.Value}"));
        var updatedHelper = provider.GetRequiredService<IEmailHelper>();
        File.Delete(_testConfigPath);

        // Assert
        Assert.Equal("Welcome Alice", originalBuilder.BuildMessage(["alice@example.com"]).Subject);
        Assert.Equal("Welcome Bob", originalHelper.ForType("Welcome").Map("Name", "Bob")
            .BuildMessage(["bob@example.com"]).Subject);
        Assert.Equal("Updated Other", updatedHelper.ForType("Welcome").Map("Name", "Other")
            .BuildMessage(["other@example.com"]).Subject);
        Assert.Throws<FileNotFoundException>(() => provider.GetRequiredService<IEmailHelper>());
    }

    [Fact]
    public void AddTemplateEmail_WithLoadedConfiguration_ShouldReuseUntilExplicitlyRegisteredAgain()
    {
        // Arrange
        var serializer = new XmlSerializer(typeof(MailConfiguration));
        MailConfiguration configuration;
        using (var stream = File.OpenRead(_testConfigPath))
        {
            configuration = (MailConfiguration)serializer.Deserialize(stream);
        }
        var services = new ServiceCollection();
        services.AddTemplateEmail(configuration, _ => { });
        using var provider = services.BuildServiceProvider();
        var originalHelper = provider.GetRequiredService<IEmailHelper>();
        var originalBuilder = originalHelper.ForType("Welcome").Map("Name", "Alice");

        // Act
        File.WriteAllText(_testConfigPath, File.ReadAllText(_testConfigPath)
            .Replace("Welcome {Name.Value}", "Updated {Name.Value}"));
        var laterHelper = provider.GetRequiredService<IEmailHelper>();
        using (var stream = File.OpenRead(_testConfigPath))
        {
            configuration = (MailConfiguration)serializer.Deserialize(stream);
        }
        var reloadedServices = new ServiceCollection();
        reloadedServices.AddTemplateEmail(configuration, _ => { });
        using var reloadedProvider = reloadedServices.BuildServiceProvider();
        File.Delete(_testConfigPath);

        // Assert
        Assert.Equal("Welcome Alice", originalBuilder.BuildMessage(["alice@example.com"]).Subject);
        Assert.Equal("Welcome Bob", laterHelper.ForType("Welcome").Map("Name", "Bob")
            .BuildMessage(["bob@example.com"]).Subject);
        Assert.Equal("Welcome Again", provider.GetRequiredService<IEmailHelper>().ForType("Welcome").Map("Name", "Again")
            .BuildMessage(["again@example.com"]).Subject);
        Assert.Equal("Updated Other", reloadedProvider.GetRequiredService<IEmailHelper>().ForType("Welcome").Map("Name", "Other")
            .BuildMessage(["other@example.com"]).Subject);
    }

    [Fact]
    public void AddTemplateEmail_WithInvalidConfiguration_ShouldValidateWhenHelperIsResolved()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, """
            <MailConfiguration>
              <MailMessage MessageType="BrokenMail">
                <MessageBody>Body</MessageBody>
              </MailMessage>
            </MailConfiguration>
            """);
        var configuration = new MailConfiguration
        {
            MailMessages = [new() { MessageType = "BrokenMail", MessageBody = "Body" }]
        };
        var servicesFromFile = new ServiceCollection();
        var servicesFromObject = new ServiceCollection();
        servicesFromFile.AddTemplateEmail(_testConfigPath, _ => { });
        servicesFromObject.AddTemplateEmail(configuration, _ => { });

        foreach (var services in new[] { servicesFromFile, servicesFromObject })
        {
            using var provider = services.BuildServiceProvider();

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IEmailHelper>());

            // Assert
            Assert.Contains("MailMessages[0]", exception.Message);
            Assert.Contains("MessageType='BrokenMail'", exception.Message);
            Assert.Contains(": MessageSubject ", exception.Message);
        }
    }

    [Fact]
    public void AddTemplateEmail_WithConfiguration_ShouldRegisterIsolatedTransientHelpersAndSingletonSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new MailConfiguration
        {
            MailMessages =
            [
                new MailMessageConfiguration
                {
                    MessageType = "Welcome",
                    MessageSubject = "Welcome {Name.Value}",
                    MessageBody = "Hello {Name.Value}"
                }
            ]
        };
        var smtpSettings = new SmtpSettings("smtp.example.com", null, null, 587);
        var configureCalls = 0;

        // Act
        var result = services.AddTemplateEmail(configuration, settings =>
        {
            configureCalls++;
            settings.FromAddress = "sender@example.com";
            settings.SmtpSettings = smtpSettings;
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var first = scope.ServiceProvider.GetRequiredService<IEmailHelper>();
        var second = scope.ServiceProvider.GetRequiredService<IEmailHelper>();
        IEmailBuilder aliceBuilder = first.ForType("Welcome").Map("Name", "Alice");
        var bobBuilder = first.ForType("Welcome").Map("Name", "Bob");
        var otherBuilder = second.ForType("Welcome").Map("Name", "Other");
        var bobMessage = bobBuilder.BuildMessage(["bob@example.com"]);
        var aliceMessage = aliceBuilder.BuildMessage(["alice@example.com"]);
        var otherMessage = otherBuilder.BuildMessage(["other@example.com"]);
        var settings = scope.ServiceProvider.GetRequiredService<EmailSettings>();

        // Assert
        Assert.Same(services, result);
        Assert.NotSame(first, second);
        Assert.Equal("Welcome Alice", aliceMessage.Subject);
        Assert.Equal("Hello Alice", aliceMessage.Body);
        Assert.Equal("Welcome Bob", bobMessage.Subject);
        Assert.Equal("Hello Bob", bobMessage.Body);
        Assert.Equal("Welcome Other", otherMessage.Subject);
        Assert.Equal("sender@example.com", settings.FromAddress);
        Assert.Same(smtpSettings, settings.SmtpSettings);
        Assert.Same(settings, provider.GetRequiredService<EmailSettings>());
        Assert.Equal(1, configureCalls);
    }
}
