using System.Globalization;
using System.Reflection;
using Xunit;

namespace Edi.TemplateEmail.Tests;

public class CultureTests
{
    private readonly MailConfiguration _configuration = new()
    {
        MailMessages =
        [
            new() { MessageType = "Notice", MessageCulture = "de-DE", MessageSubject = "First", MessageBody = "" },
            new() { MessageType = "Notice", MessageSubject = "Default", MessageBody = "" },
            new() { MessageType = "Notice", MessageCulture = "fr", MessageSubject = "Parent", MessageBody = "" },
            new() { MessageType = "Notice", MessageCulture = "FR-ca", MessageSubject = "Exact", MessageBody = "" }
        ]
    };

    [Fact]
    public void ForType_WithExplicitCulture_ShouldSelectExactParentOrDefault()
    {
        // Arrange
        IEmailHelper helper = new EmailHelper(_configuration);
        var cases = new (string Culture, string Subject)[]
        {
            ("fr-CA", "Exact"), ("fr-BE", "Parent"), ("fr", "Parent"), ("es-MX", "Default"), ("", "Default")
        };

        foreach (var item in cases)
        {
            // Act
            var message = helper.ForType("Notice", CultureInfo.GetCultureInfo(item.Culture))
                .BuildMessage(["to@example.com"]);

            // Assert
            Assert.Equal(item.Subject, message.Subject);
        }
    }

    [Fact]
    public void ForType_WithMultipleParents_ShouldPreferNearestParent()
    {
        // Arrange
        _configuration.MailMessages.AddRange(
        [
            new() { MessageType = "Notice", MessageCulture = "zh", MessageSubject = "Root parent", MessageBody = "" },
            new() { MessageType = "Notice", MessageCulture = "zh-Hant", MessageSubject = "Nearest parent", MessageBody = "" }
        ]);
        var helper = new EmailHelper(_configuration);
        var culture = CultureInfo.GetCultureInfo("zh-Hant-TW");

        // Act
        var nearest = helper.ForType("Notice", culture).BuildMessage(["to@example.com"]);
        _configuration.MailMessages.RemoveAt(_configuration.MailMessages.Count - 1);
        var root = helper.ForType("Notice", culture).BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal("Nearest parent", nearest.Subject);
        Assert.Equal("Root parent", root.Subject);
    }

    [Fact]
    public void ForType_WithEmptyCultureDefault_ShouldSelectDefault()
    {
        // Arrange
        _configuration.MailMessages[1].MessageCulture = "";
        var helper = new EmailHelper(_configuration);

        // Act
        var message = helper.ForType("Notice", CultureInfo.GetCultureInfo("es-MX"))
            .BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal("Default", message.Subject);
    }

    [Fact]
    public void BuildMessage_WithoutMatchingCultureOrDefault_ShouldReportTypeAndCulture()
    {
        // Arrange
        _configuration.MailMessages.RemoveAt(1);
        var helper = new EmailHelper(_configuration);
        var builder = helper.ForType("Notice", CultureInfo.GetCultureInfo("es-MX"));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.BuildMessage(["to@example.com"]));

        // Assert
        Assert.Contains("Notice", exception.Message);
        Assert.Contains("es-MX", exception.Message);
        Assert.Contains("no default template", exception.Message);
        Assert.Equal("Exact", helper.ForType("Notice", CultureInfo.GetCultureInfo("fr-CA"))
            .BuildMessage(["to@example.com"]).Subject);
    }

    [Fact]
    public void BuildMessage_WithDifferentMessageTypeCasing_ShouldNotMatch()
    {
        // Arrange
        var builder = new EmailHelper(_configuration).ForType("notice", CultureInfo.GetCultureInfo("fr-CA"));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.BuildMessage(["to@example.com"]));

        // Assert
        Assert.Contains("notice", exception.Message);
        Assert.Contains("fr-CA", exception.Message);
    }

    [Fact]
    public void ForType_WithNullCulture_ShouldRejectBeforeCreatingBuilder()
    {
        // Arrange
        IEmailHelper helper = new EmailHelper(_configuration);

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => helper.ForType("Notice", null));

        // Assert
        Assert.Equal("culture", exception.ParamName);
    }

    [Fact]
    public void BuildMessage_WithExplicitCulture_ShouldKeepTemplateSelectionDeferredAndBuildersIndependent()
    {
        // Arrange
        var configuration = new MailConfiguration();
        var helper = new EmailHelper(configuration);
        var french = helper.ForType("Notice", CultureInfo.GetCultureInfo("fr-CA")).Map("Name", "Alice");
        var german = helper.ForType("Notice", CultureInfo.GetCultureInfo("de-DE")).Map("Name", "Bob");

        // Act
        configuration.MailMessages.AddRange(
        [
            new() { MessageType = "Notice", MessageCulture = "fr", MessageSubject = "French {Name.Value}", MessageBody = "" },
            new() { MessageType = "Notice", MessageCulture = "de", MessageSubject = "German {Name.Value}", MessageBody = "" }
        ]);
        var germanMessage = german.BuildMessage(["bob@example.com"]);
        var frenchMessage = french.BuildMessage(["alice@example.com"]);
        var fresh = helper.ForType("Notice", CultureInfo.GetCultureInfo("fr-CA"));

        // Assert
        Assert.Equal("German Bob", germanMessage.Subject);
        Assert.Equal("French Alice", frenchMessage.Subject);
        Assert.NotSame(french, fresh);
        Assert.Equal("French", fresh.BuildMessage(["to@example.com"]).Subject);
    }

    [Fact]
    public void BuildMessage_WithParentOrDefaultTemplate_ShouldFormatUsingRecipientCulture()
    {
        // Arrange
        _configuration.MailMessages.RemoveAt(3);
        const string tokens = "{Amount.Value}|{Date.Value}|{Order.Amount}|{Order.Date}";
        foreach (var template in _configuration.MailMessages)
        {
            template.MessageBody = tokens;
            template.MessageSubject += " " + tokens;
        }
        var helper = new EmailHelper(_configuration);
        const decimal amount = 1234.5m;
        var date = new DateTime(2026, 11, 5, 14, 30, 0);
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            foreach (var name in new[] { "fr-CA", "es-MX" })
            {
                var culture = CultureInfo.GetCultureInfo(name);
                var formatted = $"{amount.ToString(culture)}|{date.ToString(culture)}";

                // Act
                var message = helper.ForType("Notice", culture)
                    .MapRange(("Amount", amount), ("Date", date), ("Order", new { Amount = amount, Date = date }))
                    .BuildMessage(["to@example.com"]);

                // Assert
                var expected = $"{formatted}|{formatted}";
                Assert.Equal(expected, message.Body);
                Assert.Equal($"{(name == "fr-CA" ? "Parent" : "Default")} {expected}", message.Subject);
                Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void BuildMessage_WithExplicitCulture_ShouldPreserveCustomToStringAndTokenRules()
    {
        // Arrange
        _configuration.MailMessages[1].MessageBody =
            "{Custom.Value}|{Order.Custom}|{Fallback.Any}|{Text.Any}|{Null.Value}|{Order.Null}|{Missing.Value}|{Order.Missing}";
        var custom = new CustomText();

        // Act
        var message = new EmailHelper(_configuration).ForType("Notice", CultureInfo.GetCultureInfo("es-MX"))
            .MapRange(("Custom", custom), ("Fallback", new PropertylessText()), ("Text", "text"), ("Null", null),
                ("Order", new { Custom = custom, Null = (object)null }))
            .BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal("custom|custom|fallback|text||||", message.Body);
    }

    [Fact]
    public void BuildMessage_WithCultureAwareValue_ShouldPreserveHtmlEncodingAndTrustedFragments()
    {
        // Arrange
        var template = _configuration.MailMessages[1];
        template.IsHtml = true;
        template.MessageSubject = "{Data.Value}";
        template.MessageBody = "<p>{Data.Value}|{Order.Data}|{Footer.Value}|{Order.Footer}</p>";
        var data = new CultureAwareText();
        var footer = new TrustedHtml("<em>Footer</em>");

        // Act
        var message = new EmailHelper(_configuration).ForType("Notice", CultureInfo.GetCultureInfo("es-MX"))
            .MapRange(("Data", data), ("Footer", footer), ("Order", new { Data = data, Footer = footer }))
            .BuildMessage(["to@example.com"]);

        // Assert
        Assert.Equal("<es-MX>", message.Subject);
        Assert.Equal("<p>&lt;es-MX&gt;|&lt;es-MX&gt;|<em>Footer</em>|&lt;em&gt;Footer&lt;/em&gt;</p>", message.Body);
        Assert.True(message.BodyIsHtml);
        Assert.Equal("legacy", new PipelineItem { Value = data }.GetValue("Value"));
        var pipeline = new TemplatePipeline().Map("Data", data);
        var engine = new TemplateEngine(new TemplateMailMessage(_configuration, "Notice"), pipeline);
        Assert.Equal("legacy", engine.Format("{Data.Value}"));
        Assert.Equal("legacy", engine.Format("{Data.Value}", true));
    }

    [Fact]
    public void BuildMessage_WithThrowingGetter_ShouldPropagateOriginalGetterException()
    {
        // Arrange
        _configuration.MailMessages[1].MessageBody = "{Order.Throwing}";
        var builder = new EmailHelper(_configuration).ForType("Notice", CultureInfo.GetCultureInfo("es-MX"))
            .Map("Order", new ThrowingModel());

        // Act
        var exception = Assert.Throws<TargetInvocationException>(() => builder.BuildMessage(["to@example.com"]));

        // Assert
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("Getter failed.", exception.InnerException.Message);
    }

    [Fact]
    public async Task BuildMessage_WithParallelCultures_ShouldKeepFormattingAndAmbientCultureIndependent()
    {
        // Arrange
        var template = _configuration.MailMessages[1];
        template.MessageSubject = "{Amount.Value}";
        template.MessageBody = "{Date.Value}";
        _configuration.MailMessages = [template];
        var helper = new EmailHelper(_configuration);
        var cultures = new[] { CultureInfo.GetCultureInfo("fr-CA"), CultureInfo.GetCultureInfo("en-US") };
        const decimal amount = 1234.5m;
        var date = new DateTime(2026, 11, 5, 14, 30, 0);
        var builders = Enumerable.Range(0, 16).Select(index => helper.ForType("Notice", cultures[index % 2])
            .MapRange(("Amount", amount), ("Date", date))).ToArray();

        // Act
        var messages = await Task.WhenAll(builders.Select(builder => Task.Run(() =>
        {
            var ambientCulture = CultureInfo.CurrentCulture;
            var message = builder.BuildMessage(["to@example.com"]);
            Assert.Same(ambientCulture, CultureInfo.CurrentCulture);
            return message;
        })));

        // Assert
        for (var index = 0; index < messages.Length; index++)
        {
            Assert.Equal(amount.ToString(cultures[index % 2]), messages[index].Subject);
            Assert.Equal(date.ToString(cultures[index % 2]), messages[index].Body);
        }
    }

    [Fact]
    public void ForType_WithoutExplicitCulture_ShouldKeepCurrentCultureAndFirstTemplateFallback()
    {
        // Arrange
        foreach (var template in _configuration.MailMessages) template.MessageBody = "{Amount.Value}";
        var helper = new EmailHelper(_configuration);
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var legacy = helper.ForType("Notice").Map("Amount", 1234.5m);
            var explicitCulture = helper.ForType("Notice", CultureInfo.GetCultureInfo("fr-CA"));

            // Act
            var fallback = legacy.BuildMessage(["to@example.com"]);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var matching = legacy.BuildMessage(["to@example.com"]);

            // Assert
            Assert.Equal("First", fallback.Subject);
            Assert.Equal("1234.5", fallback.Body);
            Assert.Equal("First", matching.Subject);
            Assert.Equal("1234,5", matching.Body);
            Assert.Equal("Exact", explicitCulture.BuildMessage(["to@example.com"]).Subject);
            Assert.Equal("de-DE", CultureInfo.CurrentCulture.Name);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void ForType_WithLegacyImplementation_ShouldKeepExistingMethodAndRejectUnsupportedOverload()
    {
        // Arrange
        IEmailHelper helper = new LegacyHelper(new EmailHelper(_configuration));

        // Act
        var message = helper.ForType("Notice").BuildMessage(["to@example.com"]);
        var exception = Assert.Throws<NotSupportedException>(() => helper.ForType("Notice", CultureInfo.InvariantCulture));

        // Assert
        Assert.NotNull(message);
        Assert.Contains("explicit cultures", exception.Message);
    }

    [Fact]
    public void ForType_WithXmlConfiguration_ShouldUseExistingCultureElementAndDefaultShape()
    {
        // Arrange
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
                <MailConfiguration>
                  <MailMessage MessageType="Notice" IsHtml="false">
                    <MessageSubject>Default</MessageSubject>
                    <MessageBody />
                  </MailMessage>
                  <MailMessage MessageType="Notice" IsHtml="false">
                    <MessageCulture>FR</MessageCulture>
                    <MessageSubject>French {Amount.Value}</MessageSubject>
                    <MessageBody />
                  </MailMessage>
                </MailConfiguration>
                """);
            var helper = new EmailHelper(path);

            // Act
            var french = helper.ForType("Notice", CultureInfo.GetCultureInfo("fr-CA"))
                .Map("Amount", 1234.5m).BuildMessage(["to@example.com"]);
            var fallback = helper.ForType("Notice", CultureInfo.GetCultureInfo("es-MX"))
                .BuildMessage(["to@example.com"]);

            // Assert
            Assert.Equal("French 1234,5", french.Subject);
            Assert.Equal("Default", fallback.Subject);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class LegacyHelper(IEmailHelper inner) : IEmailHelper
    {
        public IEmailBuilder ForType(string mailType) => inner.ForType(mailType);
    }

    private sealed class CustomText
    {
        public string Value => "Property value";
        public override string ToString() => "custom";
    }

    private sealed class PropertylessText
    {
        public override string ToString() => "fallback";
    }

    private sealed class CultureAwareText : IFormattable
    {
        public override string ToString() => "legacy";
        public string ToString(string format, IFormatProvider formatProvider)
        {
            Assert.Null(format);
            return $"<{((CultureInfo)formatProvider).Name}>";
        }
    }

    private sealed class ThrowingModel
    {
        public string Throwing => throw new InvalidOperationException("Getter failed.");
    }
}
