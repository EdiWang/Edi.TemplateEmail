using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.DependencyInjection;
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
