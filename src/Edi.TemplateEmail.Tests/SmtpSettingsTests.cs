using Edi.TemplateEmail.Smtp;
using MailKit.Security;
using Xunit;

namespace Edi.TemplateEmail.Tests;

public class SmtpSettingsTests
{
    [Fact]
    public void Constructor_ShouldRequireStartTlsAndValidateCertificatesByDefault()
    {
        // Act
        var settings = new SmtpSettings("smtp.example.com", "user", "password", 587);

        // Assert
        Assert.Equal(SecureSocketOptions.StartTls, settings.SocketOptions);
        Assert.False(settings.SkipCertificateValidation);
    }
}
