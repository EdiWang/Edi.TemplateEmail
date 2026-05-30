using Edi.TemplateEmail.Smtp;
using MimeKit;
using MimeKit.Text;
using Xunit;

namespace Edi.TemplateEmail.Tests;

public class SmtpExtensionsTests
{
    private readonly EmailSettings _settings = new()
    {
        EmailDisplayName = "Display Name",
        SenderName = "Sender Name",
        SmtpSettings = new("smtp.example.com", "sender@example.com", "password", 587)
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
        Assert.Equal("sender@example.com", from.Address);
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
}
