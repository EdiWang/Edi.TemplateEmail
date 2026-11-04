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
        first.ForType("Welcome").Map("Name", "Alice");
        second.ForType("Welcome").Map("Name", "Bob");
        var bobMessage = second.BuildMessage(["bob@example.com"]);
        var aliceMessage = first.BuildMessage(["alice@example.com"]);
        var settings = scope.ServiceProvider.GetRequiredService<EmailSettings>();

        // Assert
        Assert.Same(services, result);
        Assert.NotSame(first, second);
        Assert.Equal("Welcome Alice", aliceMessage.Subject);
        Assert.Equal("Hello Alice", aliceMessage.Body);
        Assert.Equal("Welcome Bob", bobMessage.Subject);
        Assert.Equal("Hello Bob", bobMessage.Body);
        Assert.Equal("sender@example.com", settings.FromAddress);
        Assert.Same(smtpSettings, settings.SmtpSettings);
        Assert.Same(settings, provider.GetRequiredService<EmailSettings>());
        Assert.Equal(1, configureCalls);
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
        first.ForType("Welcome").Map("Name", "Alice");
        second.ForType("Welcome").Map("Name", "Bob");
        var bobMessage = second.BuildMessage(["bob@example.com"]);
        var aliceMessage = first.BuildMessage(["alice@example.com"]);
        var settings = scope.ServiceProvider.GetRequiredService<EmailSettings>();

        // Assert
        Assert.Same(services, result);
        Assert.NotSame(first, second);
        Assert.Equal("Welcome Alice", aliceMessage.Subject);
        Assert.Equal("Hello Alice", aliceMessage.Body);
        Assert.Equal("Welcome Bob", bobMessage.Subject);
        Assert.Equal("Hello Bob", bobMessage.Body);
        Assert.Equal("sender@example.com", settings.FromAddress);
        Assert.Same(smtpSettings, settings.SmtpSettings);
        Assert.Same(settings, provider.GetRequiredService<EmailSettings>());
        Assert.Equal(1, configureCalls);
    }
}
