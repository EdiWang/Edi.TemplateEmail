using Xunit;

namespace Edi.TemplateEmail.Tests;

public class HtmlEncodingTests
{
    private readonly MailConfiguration _configuration = new()
    {
        MailMessages =
        [
            new MailMessageConfiguration
            {
                MessageType = "HtmlMail",
                IsHtml = true,
                MessageSubject = "Subject: {Text.Value}",
                MessageBody = "<p>{Text.Value}</p>"
            },
            new MailMessageConfiguration
            {
                MessageType = "PlainMail",
                MessageSubject = "Subject: {Text.Value}",
                MessageBody = "Body: {Text.Value}"
            }
        ]
    };

    [Fact]
    public void BuildMessage_WithHtmlCharacters_ShouldEncodeOnlyInsertedBodyValues()
    {
        // Arrange
        const string text = "A&B <\"quoted\"> 'single'";

        // Act
        var message = new EmailHelper(_configuration).ForType("HtmlMail")
            .Map("Text", text).BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal("<p>A&amp;B &lt;&quot;quoted&quot;&gt; &#39;single&#39;</p>", message.Body);
        Assert.Equal($"Subject: {text}", message.Subject);
        Assert.True(message.BodyIsHtml);
    }

    [Fact]
    public void BuildMessage_WithUntrustedObjectProperty_ShouldRenderMarkupAsText()
    {
        // Arrange
        _configuration.MailMessages[0].MessageBody = "<div>{User.Name}</div>";
        var user = new { Name = "<a href=\"https://untrusted.example/\">Open</a>" };

        // Act
        var message = new EmailHelper(_configuration).ForType("HtmlMail")
            .Map("User", user).BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal("<div>&lt;a href=&quot;https://untrusted.example/&quot;&gt;Open&lt;/a&gt;</div>", message.Body);
    }

    [Fact]
    public void BuildMessage_WithPlainTextTemplate_ShouldPreserveSubjectAndBodyValues()
    {
        // Arrange
        const string text = "<b>A & B</b>";

        // Act
        var message = new EmailHelper(_configuration).ForType("PlainMail")
            .Map("Text", text).BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal($"Subject: {text}", message.Subject);
        Assert.Equal($"Body: {text}", message.Body);
        Assert.False(message.BodyIsHtml);
    }

    [Fact]
    public void BuildMessage_WithTrustedHtml_ShouldPreserveExplicitMarkupAndSubjectValue()
    {
        // Arrange
        const string html = "<strong>Approved & ready</strong>";

        // Act
        var message = new EmailHelper(_configuration).ForType("HtmlMail")
            .Map("Text", new TrustedHtml(html)).BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal($"<p>{html}</p>", message.Body);
        Assert.Equal($"Subject: {html}", message.Subject);
    }

    [Fact]
    public void BuildMessage_WithMapRangeReplacingTrustedValue_ShouldEncodeReplacement()
    {
        // Arrange
        _configuration.MailMessages[0].MessageBody = "{Text.Value}|{Other.Value}";
        var builder = new EmailHelper(_configuration).ForType("HtmlMail")
            .Map("Text", new TrustedHtml("<b>Old</b>"));

        // Act
        var message = builder.MapRange(
                ("Text", "<b>New</b>"),
                ("Other", new TrustedHtml("<em>Trusted</em>")))
            .BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal("&lt;b&gt;New&lt;/b&gt;|<em>Trusted</em>", message.Body);
    }

    [Fact]
    public void BuildMessage_WithTrustedHtmlInPlainText_ShouldPreserveItsText()
    {
        // Arrange
        const string html = "<b>A & B</b>";

        // Act
        var message = new EmailHelper(_configuration).ForType("PlainMail")
            .Map("Text", new TrustedHtml(html)).BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal($"Subject: {html}", message.Subject);
        Assert.Equal($"Body: {html}", message.Body);
    }

    [Fact]
    public void BuildMessage_WithMissingAndNullValues_ShouldKeepEmptyOutput()
    {
        // Arrange
        _configuration.MailMessages[0].MessageBody =
            "{Missing.Value}|{Null.Value}|{User.Name}|{User.Unknown}|{Raw.Value}";

        // Act
        var message = new EmailHelper(_configuration).ForType("HtmlMail")
            .MapRange(("Null", null), ("User", new { Name = (string)null }), ("Raw", new TrustedHtml(null)))
            .BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal("||||", message.Body);
    }

    [Fact]
    public void BuildMessage_WithTokensInsideValues_ShouldNotRenderThemRecursively()
    {
        // Arrange
        _configuration.MailMessages[0].MessageBody = "{Text.Value}|{Raw.Value}";

        // Act
        var message = new EmailHelper(_configuration).ForType("HtmlMail")
            .MapRange(
                ("Text", "{Raw.Value}<b>Text</b>"),
                ("Raw", new TrustedHtml("<b>{Text.Value}</b>")))
            .BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal("{Raw.Value}&lt;b&gt;Text&lt;/b&gt;|<b>{Text.Value}</b>", message.Body);
    }

    [Fact]
    public void BuildMessage_WithTrustedHtmlInsideOrdinaryObject_ShouldEncodePropertyValue()
    {
        // Arrange
        _configuration.MailMessages[0].MessageBody = "<div>{User.Content}</div>";

        // Act
        var message = new EmailHelper(_configuration).ForType("HtmlMail")
            .Map("User", new { Content = new TrustedHtml("<b>Nested</b>") })
            .BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal("<div>&lt;b&gt;Nested&lt;/b&gt;</div>", message.Body);
    }

    [Fact]
    public void BuildMessage_WithPreEncodedOrdinaryText_ShouldEncodeAmpersands()
    {
        // Act
        var message = new EmailHelper(_configuration).ForType("HtmlMail")
            .Map("Text", "&lt;b&gt;").BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal("<p>&amp;lt;b&amp;gt;</p>", message.Body);
    }

    [Fact]
    public void BuildMessage_WithQuotedAttributes_ShouldEncodeUrlSeparatorsAndQuotes()
    {
        // Arrange
        _configuration.MailMessages[0].MessageBody = "<a href=\"{Url.Value}\" title=\"{Title.Value}\">{Text.Value}</a>";

        // Act
        var message = new EmailHelper(_configuration).ForType("HtmlMail")
            .MapRange(
                ("Url", "https://example.com/?a=1&b=2"),
                ("Title", "\"quoted\" 'title'"),
                ("Text", "<Open>"))
            .BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal("<a href=\"https://example.com/?a=1&amp;b=2\" title=\"&quot;quoted&quot; &#39;title&#39;\">&lt;Open&gt;</a>", message.Body);
    }

    [Fact]
    public void Format_WithExplicitHtmlEncoding_ShouldPreserveDefaultFormatBehavior()
    {
        // Arrange
        var pipeline = new TemplatePipeline().Map("Text", "<b>A&B</b>");
        var engine = new TemplateEngine(new TemplateMailMessage(_configuration, "HtmlMail"), pipeline);

        // Act
        var ordinary = engine.Format("<p>{Text.Value}</p>");
        var encoded = engine.Format("<p>{Text.Value}</p>", encodeHtml: true);

        // Assert
        Assert.Equal("<p><b>A&B</b></p>", ordinary);
        Assert.Equal("<p>&lt;b&gt;A&amp;B&lt;/b&gt;</p>", encoded);
    }
}
