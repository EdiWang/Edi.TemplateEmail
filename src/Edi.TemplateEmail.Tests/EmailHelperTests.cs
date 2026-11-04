using System.Xml.Serialization;
using Xunit;

namespace Edi.TemplateEmail.Tests;

public class EmailHelperTests : IDisposable
{
    private readonly MailConfiguration _testMailConfiguration;
    private readonly string _testConfigPath;

    public EmailHelperTests()
    {
        _testMailConfiguration = new MailConfiguration
        {
            MailMessages =
            [
                new MailMessageConfiguration
                {
                    MessageType = "TestMail",
                    IsHtml = true,
                    MessageSubject = "Test Subject: {MachineName.Value}",
                    MessageBody = "Test Body: {MachineName.Value} - {SmtpServer.Value}"
                }
            ]
        };

        // Create a temporary XML config file for testing
        _testConfigPath = Path.GetTempFileName();
        var serializer = new XmlSerializer(typeof(MailConfiguration));
        using var fileStream = new FileStream(_testConfigPath, FileMode.Create);
        serializer.Serialize(fileStream, _testMailConfiguration);
    }

    public void Dispose()
    {
        if (File.Exists(_testConfigPath))
        {
            File.Delete(_testConfigPath);
        }
    }

    [Fact]
    public void Constructor_WithConfiguration_ShouldLoadTemplate()
    {
        // Act
        var emailHelper = new EmailHelper(_testMailConfiguration);
        var message = emailHelper.ForType("TestMail").Map("MachineName", "Alice")
            .BuildMessage(["alice@example.com"]);

        // Assert
        Assert.Equal("Test Subject: Alice", message.Subject);
    }

    [Fact]
    public void Constructor_WithConfigPath_ShouldLoadTemplate()
    {
        // Act
        var emailHelper = new EmailHelper(_testConfigPath);
        var message = emailHelper.ForType("TestMail").Map("MachineName", "Alice")
            .BuildMessage(["alice@example.com"]);

        // Assert
        Assert.Equal("Test Subject: Alice", message.Subject);
    }

    [Fact]
    public void Constructor_WithHandWrittenXml_ShouldLoadSerializedShape()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, """
            <MailConfiguration>
              <MailMessage MessageType="PlainMail" IsHtml="false">
                <MessageSubject>Plain subject</MessageSubject>
                <MessageBody>Plain body</MessageBody>
                <MessageCulture>fr-FR</MessageCulture>
              </MailMessage>
              <MailMessage MessageType="HtmlMail" IsHtml="true">
                <MessageSubject>Welcome {Name.Value}</MessageSubject>
                <MessageBody><![CDATA[<p>Hello {Name.Value}</p>]]></MessageBody>
                <MessageCulture>en-US</MessageCulture>
              </MailMessage>
            </MailConfiguration>
            """);

        // Act
        MailConfiguration configuration;
        using (var stream = File.OpenRead(_testConfigPath))
        {
            configuration = (MailConfiguration)new XmlSerializer(typeof(MailConfiguration)).Deserialize(stream);
        }
        var helper = new EmailHelper(_testConfigPath);
        var message = helper.ForType("HtmlMail")
            .Map("Name", "Alice")
            .BuildMessage(["alice@example.com"]);
        var plainMessage = helper.ForType("PlainMail").BuildMessage(["alice@example.com"]);

        // Assert
        Assert.Equal(2, configuration.MailMessages.Count);
        Assert.Equal("fr-FR", configuration.MailMessages[0].MessageCulture);
        Assert.Equal("en-US", configuration.MailMessages[1].MessageCulture);
        Assert.Equal("Welcome Alice", message.Subject);
        Assert.Equal("<p>Hello Alice</p>", message.Body);
        Assert.True(message.BodyIsHtml);
        Assert.Equal("Plain subject", plainMessage.Subject);
        Assert.Equal("Plain body", plainMessage.Body);
        Assert.False(plainMessage.BodyIsHtml);
    }

    [Fact]
    public void Constructor_WithReadOnlyConfigFile_ShouldLoadTemplate()
    {
        // Arrange
        var originalAttributes = File.GetAttributes(_testConfigPath);
        File.SetAttributes(_testConfigPath, originalAttributes | FileAttributes.ReadOnly);

        try
        {
            // Act
            var message = new EmailHelper(_testConfigPath)
                .ForType("TestMail")
                .Map("MachineName", "TestMachine")
                .BuildMessage(["test@example.com"]);

            // Assert
            Assert.Equal("Test Subject: TestMachine", message.Subject);
        }
        finally
        {
            File.SetAttributes(_testConfigPath, originalAttributes);
        }
    }

    [Fact]
    public void Constructor_WithConfigFileOpenForReading_ShouldLoadTemplate()
    {
        // Arrange
        using var reader = File.OpenRead(_testConfigPath);

        // Act
        var message = new EmailHelper(_testConfigPath)
            .ForType("TestMail")
            .Map("MachineName", "TestMachine")
            .BuildMessage(["test@example.com"]);

        // Assert
        Assert.Equal("Test Subject: TestMachine", message.Subject);
    }

    [Fact]
    public void Constructor_WithNullConfigPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailHelper((string)null));
    }

    [Fact]
    public void Constructor_WithEmptyConfigPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailHelper(string.Empty));
    }

    [Fact]
    public void Constructor_WithWhitespaceConfigPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailHelper("   "));
    }

    [Fact]
    public void Constructor_WithNonExistentConfigPath_ShouldThrowFileNotFoundException()
    {
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => new EmailHelper("nonexistent.xml"));
    }

    [Fact]
    public void ForType_ShouldCreateFreshBuilders()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);
        const string mailType = "TestMail";

        // Act
        var result = emailHelper.ForType(mailType);

        // Assert
        Assert.NotSame(result, emailHelper.ForType(mailType));
    }

    [Fact]
    public void ForType_AfterBuild_ShouldStartWithFreshMappings()
    {
        // Arrange
        _testMailConfiguration.MailMessages.Add(new MailMessageConfiguration
        {
            MessageType = "OtherMail",
            MessageSubject = "Other: {MachineName.Value}",
            MessageBody = "Other body: {SmtpServer.Value}"
        });
        var helper = new EmailHelper(_testMailConfiguration);
        var firstBuilder = helper.ForType("TestMail")
            .Map("MachineName", "Alice")
            .Map("SmtpServer", "old.example.com");
        firstBuilder.BuildMessage(["alice@example.com"]);

        // Act
        var secondBuilder = helper.ForType("OtherMail").Map("MachineName", "Bob");
        var message = secondBuilder.BuildMessage(["bob@example.com"]);

        // Assert
        Assert.Equal("Other: Bob", message.Subject);
        Assert.Equal("Other body:", message.Body);
        Assert.Equal("Test Body: Alice - old.example.com",
            firstBuilder.BuildMessage(["alice@example.com"]).Body);
    }

    [Fact]
    public void BuildMessage_WithUnknownTemplateType_ShouldThrowAtBuildTime()
    {
        // Arrange
        var helper = new EmailHelper(_testMailConfiguration);
        var builder = helper.ForType("UnknownMail").Map("MachineName", "Alice");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder.BuildMessage(["alice@example.com"]));

        // Assert
        Assert.Equal("Mail type 'UnknownMail' was not found in the mail configuration.", exception.Message);
    }

    [Fact]
    public void Map_ShouldAddValueAndReturnBuilder()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);
        var builder = emailHelper.ForType("TestMail");
        const string name = "MachineName";
        const string value = "TestValue";

        // Act
        var result = builder.Map(name, value);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal("Test Subject: TestValue", result.BuildMessage(["test@example.com"]).Subject);
    }

    [Fact]
    public void MapRange_ShouldAddMultipleValuesAndReturnBuilder()
    {
        // Arrange
        _testMailConfiguration.MailMessages[0].MessageSubject = "{Name1.Value}|{Name2.Value}|{Name3.Value}";
        var builder = new EmailHelper(_testMailConfiguration).ForType("TestMail");
        var values = new (string name, object value)[]
        {
            ("Name1", "Value1"),
            ("Name2", 42),
            ("Name3", true)
        };

        // Act
        var result = builder.MapRange(values);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal("Value1|42|True", result.BuildMessage(["test@example.com"]).Subject);
    }

    [Fact]
    public void MapRange_WithNoValues_ShouldReturnSelf()
    {
        // Arrange
        var builder = new EmailHelper(_testMailConfiguration).ForType("TestMail");

        // Act
        var result = builder.MapRange();

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void BuildMessage_WithValidReceipts_ShouldReturnCommonMailMessage()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);
        var receipts = new[] { "test1@example.com", "test2@example.com" };
        var ccReceipts = new[] { "cc1@example.com" };

        var builder = emailHelper.ForType("TestMail")
                  .Map("MachineName", Environment.MachineName)
                  .Map("SmtpServer", "smtp.test.com");

        // Act
        var result = builder.BuildMessage(receipts, ccReceipts);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<CommonMailMessage>(result);
        Assert.NotSame(receipts, result.Receipts);
        Assert.Equal(receipts, result.Receipts);
        Assert.NotSame(ccReceipts, result.CcReceipts);
        Assert.Equal(ccReceipts, result.CcReceipts);
        Assert.NotNull(result.Subject);
        Assert.NotNull(result.Body);
        Assert.True(result.BodyIsHtml);
    }

    [Fact]
    public void BuildMessage_WithOnlyReceipts_ShouldReturnCommonMailMessage()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);
        var receipts = new[] { "test@example.com" };

        var builder = emailHelper.ForType("TestMail")
                  .Map("MachineName", Environment.MachineName)
                  .Map("SmtpServer", "smtp.test.com");

        // Act
        var result = builder.BuildMessage(receipts);

        // Assert
        Assert.NotNull(result);
        Assert.NotSame(receipts, result.Receipts);
        Assert.Equal(receipts, result.Receipts);
        Assert.Null(result.CcReceipts);
    }

    [Fact]
    public void BuildMessage_WithNullReceipts_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = new EmailHelper(_testMailConfiguration).ForType("TestMail");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.BuildMessage(null));
    }

    [Fact]
    public void BuildMessage_WithEmptyReceipts_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = new EmailHelper(_testMailConfiguration).ForType("TestMail");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.BuildMessage([]));
    }

    [Fact]
    public void FluentInterface_ShouldWorkCorrectly()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);
        var receipts = new[] { "test@example.com" };

        // Act
        var result = emailHelper
            .ForType("TestMail")
            .Map("MachineName", Environment.MachineName)
            .MapRange(
                ("SmtpServer", "smtp.test.com"),
                ("Port", 587)
            )
            .BuildMessage(receipts);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<CommonMailMessage>(result);
        Assert.Equal($"Test Subject: {Environment.MachineName}", result.Subject);
        Assert.Equal($"Test Body: {Environment.MachineName} - smtp.test.com", result.Body);
    }

    [Fact]
    public void BuildMessage_ShouldDeferTemplateSelectionUntilBuild()
    {
        // Arrange
        var configuration = new MailConfiguration();
        var builder = new EmailHelper(configuration).ForType("DeferredMail")
            .Map("Name", "Alice");

        // Act
        configuration.MailMessages.Add(new MailMessageConfiguration
        {
            MessageType = "DeferredMail",
            MessageSubject = "Hello {Name.Value}",
            MessageBody = "Deferred body"
        });
        var message = builder.BuildMessage(["alice@example.com"]);

        // Assert
        Assert.Equal("Hello Alice", message.Subject);
        Assert.Equal("Deferred body", message.Body);
    }

    [Fact]
    public void BuildMessage_ShouldTrimSubjectAndBody()
    {
        // Arrange
        var configWithWhitespace = new MailConfiguration
        {
            MailMessages =
            [
                new MailMessageConfiguration
                {
                    MessageType = "TestMail",
                    IsHtml = false,
                    MessageSubject = "  Test Subject  ",
                    MessageBody = "  Test Body  "
                }
            ]
        };

        var emailHelper = new EmailHelper(configWithWhitespace);
        var receipts = new[] { "test@example.com" };

        var builder = emailHelper.ForType("TestMail");

        // Act
        var result = builder.BuildMessage(receipts);

        // Assert
        Assert.Equal("Test Subject", result.Subject);
        Assert.Equal("Test Body", result.Body);
    }

    [Fact]
    public void ForType_WithInterleavedBuilders_ShouldKeepMessageTypesAndMappingsIndependent()
    {
        // Arrange
        _testMailConfiguration.MailMessages.Add(new MailMessageConfiguration
        {
            MessageType = "OtherMail",
            MessageSubject = "Other: {MachineName.Value}",
            MessageBody = "Server: {SmtpServer.Value}"
        });
        var helper = new EmailHelper(_testMailConfiguration);

        // Act
        var aliceBuilder = helper.ForType("TestMail").Map("MachineName", "Alice");
        var bobBuilder = helper.ForType("OtherMail")
            .MapRange(("MachineName", "Bob"), ("SmtpServer", "bob.example.com"));
        aliceBuilder.Map("SmtpServer", "alice.example.com");
        var bobMessage = bobBuilder.BuildMessage(["bob@example.com"]);
        var aliceMessage = aliceBuilder.BuildMessage(["alice@example.com"]);

        // Assert
        Assert.NotSame(aliceBuilder, bobBuilder);
        Assert.Equal("Test Subject: Alice", aliceMessage.Subject);
        Assert.Equal("Test Body: Alice - alice.example.com", aliceMessage.Body);
        Assert.True(aliceMessage.BodyIsHtml);
        Assert.Equal("Other: Bob", bobMessage.Subject);
        Assert.Equal("Server: bob.example.com", bobMessage.Body);
        Assert.False(bobMessage.BodyIsHtml);
    }

    [Fact]
    public async Task BuildMessage_WithParallelIndependentBuilders_ShouldKeepMappingsIndependent()
    {
        // Arrange
        var helper = new EmailHelper(_testMailConfiguration);
        var builders = Enumerable.Range(0, 16)
            .Select(index => helper.ForType("TestMail")
                .MapRange(("MachineName", $"Machine{index}"), ("SmtpServer", $"smtp{index}.example.com")))
            .ToArray();

        // Act
        var messages = await Task.WhenAll(builders.Select((builder, index) =>
            Task.Run(() => builder.BuildMessage([$"user{index}@example.com"]))));

        // Assert
        for (var index = 0; index < messages.Length; index++)
        {
            Assert.Equal($"Test Subject: Machine{index}", messages[index].Subject);
            Assert.Equal($"Test Body: Machine{index} - smtp{index}.example.com", messages[index].Body);
            Assert.Equal($"user{index}@example.com", Assert.Single(messages[index].Receipts));
        }
    }

    [Fact]
    public void BuildMessage_ShouldSnapshotRecipientArraysForEachMessage()
    {
        // Arrange
        var builder = new EmailHelper(_testMailConfiguration).ForType("TestMail");
        string[] receipts = ["alice@example.com"];
        string[] ccReceipts = ["cc@example.com"];

        // Act
        var first = builder.BuildMessage(receipts, ccReceipts);
        receipts[0] = "bob@example.com";
        ccReceipts[0] = "other-cc@example.com";
        var second = builder.BuildMessage(receipts, ccReceipts);
        second.Receipts[0] = "changed@example.com";
        second.CcReceipts[0] = "changed-cc@example.com";

        // Assert
        Assert.Equal("alice@example.com", Assert.Single(first.Receipts));
        Assert.Equal("cc@example.com", Assert.Single(first.CcReceipts));
        Assert.Equal("bob@example.com", Assert.Single(receipts));
        Assert.Equal("other-cc@example.com", Assert.Single(ccReceipts));
    }

    [Fact]
    public void BuildMessage_ShouldSetBodyIsHtmlFromConfiguration()
    {
        // Arrange
        var htmlConfig = new MailConfiguration
        {
            MailMessages =
            [
                new MailMessageConfiguration
                {
                    MessageType = "HtmlMail",
                    IsHtml = true,
                    MessageSubject = "HTML Subject",
                    MessageBody = "<h1>HTML Body</h1>"
                }
            ]
        };

        var textConfig = new MailConfiguration
        {
            MailMessages =
            [
                new MailMessageConfiguration
                {
                    MessageType = "TextMail",
                    IsHtml = false,
                    MessageSubject = "Text Subject",
                    MessageBody = "Text Body"
                }
            ]
        };

        var emailHelperHtml = new EmailHelper(htmlConfig);
        var emailHelperText = new EmailHelper(textConfig);
        var receipts = new[] { "test@example.com" };

        // Act
        var htmlResult = emailHelperHtml.ForType("HtmlMail").BuildMessage(receipts);
        var textResult = emailHelperText.ForType("TextMail").BuildMessage(receipts);

        // Assert
        Assert.True(htmlResult.BodyIsHtml);
        Assert.False(textResult.BodyIsHtml);
    }
}
