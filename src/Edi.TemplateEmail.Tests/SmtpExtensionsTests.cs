using Edi.TemplateEmail.Smtp;
using MimeKit;
using MimeKit.Text;
using Xunit;

namespace Edi.TemplateEmail.Tests;

public class SmtpExtensionsTests
{
    private readonly EmailSettings _settings = new()
    {
        FromAddress = "from@example.com",
        EmailDisplayName = "Display Name",
        SenderAddress = "sender@example.com",
        SenderName = "Sender Name",
        SmtpSettings = new("smtp.example.com", "apikey", "password", 587)
    };

    [Fact]
    public void ToMimeMessage_WithHtmlMessage_ShouldMapSubjectBodySenderAndRecipients()
    {
        // Arrange
        var message = new CommonMailMessage
        {
            Subject = "Test Subject",
            Body = "<h1>Test Body</h1>",
            BodyIsHtml = true,
            Receipts = ["to1@example.com", "to2@example.com"],
            CcReceipts = ["cc@example.com"]
        };

        // Act
        var result = message.ToMimeMessage(_settings);

        // Assert
        Assert.Equal("Test Subject", result.Subject);
        Assert.Equal("Sender Name", result.Sender.Name);
        Assert.Equal("sender@example.com", result.Sender.Address);
        var from = Assert.Single(result.From.Mailboxes);
        Assert.Equal("Display Name", from.Name);
        Assert.Equal("from@example.com", from.Address);
        Assert.Equal(2, result.To.Mailboxes.Count());
        Assert.Contains(result.To.Mailboxes, mailbox => mailbox.Address == "to1@example.com");
        Assert.Contains(result.To.Mailboxes, mailbox => mailbox.Address == "to2@example.com");
        var cc = Assert.Single(result.Cc.Mailboxes);
        Assert.Equal("cc@example.com", cc.Address);
        var body = Assert.IsType<TextPart>(result.Body);
        Assert.Equal("html", body.ContentType.MediaSubtype);
        Assert.Equal("<h1>Test Body</h1>", body.Text);
    }

    [Fact]
    public void ToMimeMessage_WithPlainTextMessageAndNoCc_ShouldMapPlainTextBody()
    {
        // Arrange
        var message = new CommonMailMessage
        {
            Subject = "Test Subject",
            Body = "Test Body",
            BodyIsHtml = false,
            Receipts = ["to@example.com"]
        };

        // Act
        var result = message.ToMimeMessage(_settings);

        // Assert
        Assert.Empty(result.Cc);
        var body = Assert.IsType<TextPart>(result.Body);
        Assert.Equal("plain", body.ContentType.MediaSubtype);
        Assert.Equal("Test Body", body.Text);
    }

    [Fact]
    public void ToMimeMessage_WithAnonymousSmtp_ShouldUseConfiguredFromAndOmitSender()
    {
        // Arrange
        _settings.SmtpSettings = new("smtp.example.com", null, null, 25);
        _settings.SenderAddress = null;
        var message = new CommonMailMessage
        {
            Subject = "Test Subject",
            Body = "Test Body",
            Receipts = ["to@example.com"]
        };

        // Act
        var result = message.ToMimeMessage(_settings);

        // Assert
        var from = Assert.Single(result.From.Mailboxes);
        Assert.Equal("from@example.com", from.Address);
        Assert.Equal("Display Name", from.Name);
        Assert.Null(result.Sender);
    }

    [Fact]
    public void ToMimeMessage_WithoutSmtpSettings_ShouldBuildMimeMessage()
    {
        // Arrange
        var settings = new EmailSettings { FromAddress = "from@example.com" };
        var message = new CommonMailMessage
        {
            Subject = "Test Subject",
            Body = "Test Body",
            Receipts = ["to@example.com"]
        };

        // Act
        var result = message.ToMimeMessage(settings);

        // Assert
        Assert.Equal("from@example.com", Assert.Single(result.From.Mailboxes).Address);
        Assert.Null(result.Sender);
    }

    [Fact]
    public void ToMimeMessage_WithoutFromAddress_ShouldNotFallBackToSmtpUserName()
    {
        // Arrange
        _settings.FromAddress = null;
        _settings.SmtpSettings = new("smtp.example.com", "login@example.com", "password", 587);
        var message = new CommonMailMessage
        {
            Subject = "Test Subject",
            Body = "Test Body",
            Receipts = ["to@example.com"]
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => message.ToMimeMessage(_settings));
        Assert.Equal(nameof(EmailSettings.FromAddress), exception.ParamName);
    }

    [Fact]
    public void ToMimeMessage_WithWhitespaceFromAddress_ShouldThrowArgumentException()
    {
        // Arrange
        _settings.FromAddress = " ";
        var message = new CommonMailMessage
        {
            Subject = "Test Subject",
            Body = "Test Body",
            Receipts = ["to@example.com"]
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => message.ToMimeMessage(_settings));
        Assert.Equal(nameof(EmailSettings.FromAddress), exception.ParamName);
    }
}
