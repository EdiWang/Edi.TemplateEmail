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
    public void Constructor_WithConfiguration_ShouldSetProperties()
    {
        // Act
        var emailHelper = new EmailHelper(_testMailConfiguration);

        // Assert
        Assert.Null(emailHelper.Engine);
        Assert.Null(emailHelper.Pipeline);
    }

    [Fact]
    public void Constructor_WithConfigPath_ShouldSetProperties()
    {
        // Act
        var emailHelper = new EmailHelper(_testConfigPath);

        // Assert
        Assert.Null(emailHelper.Engine);
        Assert.Null(emailHelper.Pipeline);
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
    public void ForType_ShouldSetMailTypeAndCreatePipeline()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);
        const string mailType = "TestMail";

        // Act
        var result = emailHelper.ForType(mailType);

        // Assert
        Assert.Same(emailHelper, result); // Should return self for fluent interface
        Assert.NotNull(emailHelper.Pipeline);
    }

    [Fact]
    public void ForType_AfterBuild_ShouldResetMappingsAndDeferEngineCreation()
    {
        // Arrange
        _testMailConfiguration.MailMessages.Add(new MailMessageConfiguration
        {
            MessageType = "OtherMail",
            MessageSubject = "Other: {MachineName.Value}",
            MessageBody = "Other body: {SmtpServer.Value}"
        });
        var helper = new EmailHelper(_testMailConfiguration);
        helper.ForType("TestMail")
            .Map("MachineName", "Alice")
            .Map("SmtpServer", "old.example.com")
            .BuildMessage(["alice@example.com"]);
        var previousPipeline = helper.Pipeline;
        Assert.NotNull(helper.Engine);

        // Act
        helper.ForType("OtherMail");

        // Assert
        Assert.NotSame(previousPipeline, helper.Pipeline);
        Assert.False(helper.Pipeline.HasEntity("MachineName"));
        Assert.False(helper.Pipeline.HasEntity("SmtpServer"));
        Assert.Null(helper.Engine);
        helper.Map("MachineName", "Bob");
        Assert.Null(helper.Engine);
        var message = helper.BuildMessage(["bob@example.com"]);
        Assert.Equal("Other: Bob", message.Subject);
        Assert.Equal("Other body:", message.Body);
    }

    [Fact]
    public void BuildMessage_WithUnknownTemplateType_ShouldThrowAtBuildTime()
    {
        // Arrange
        var helper = new EmailHelper(_testMailConfiguration);
        helper.ForType("UnknownMail").Map("MachineName", "Alice");
        Assert.Null(helper.Engine);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            helper.BuildMessage(["alice@example.com"]));

        // Assert
        Assert.Equal("Mail type 'UnknownMail' was not found in the mail configuration.", exception.Message);
        Assert.Null(helper.Engine);
    }

    [Fact]
    public void Map_ShouldAddValueToPipeline()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);
        emailHelper.ForType("TestMail");
        const string name = "TestName";
        const string value = "TestValue";

        // Act
        var result = emailHelper.Map(name, value);

        // Assert
        Assert.Same(emailHelper, result); // Should return self for fluent interface
        Assert.True(emailHelper.Pipeline.HasEntity(name));
        Assert.Equal(value, emailHelper.Pipeline[name].Value);
    }

    [Fact]
    public void Map_WithoutForType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => emailHelper.Map("TestName", "TestValue"));
    }

    [Fact]
    public void MapRange_ShouldAddMultipleValuesToPipeline()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);
        emailHelper.ForType("TestMail");
        var values = new (string name, object value)[]
        {
            ("Name1", "Value1"),
            ("Name2", 42),
            ("Name3", true)
        };

        // Act
        var result = emailHelper.MapRange(values);

        // Assert
        Assert.Same(emailHelper, result); // Should return self for fluent interface
        Assert.True(emailHelper.Pipeline.HasEntity("Name1"));
        Assert.True(emailHelper.Pipeline.HasEntity("Name2"));
        Assert.True(emailHelper.Pipeline.HasEntity("Name3"));
        Assert.Equal("Value1", emailHelper.Pipeline["Name1"].Value);
        Assert.Equal(42, emailHelper.Pipeline["Name2"].Value);
        Assert.Equal(true, emailHelper.Pipeline["Name3"].Value);
    }

    [Fact]
    public void MapRange_WithNoValues_ShouldReturnSelf()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);
        emailHelper.ForType("TestMail");

        // Act
        var result = emailHelper.MapRange();

        // Assert
        Assert.Same(emailHelper, result);
    }

    [Fact]
    public void MapRange_WithoutForType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => emailHelper.MapRange(("TestName", "TestValue")));
    }

    [Fact]
    public void BuildMessage_WithValidReceipts_ShouldReturnCommonMailMessage()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);
        var receipts = new[] { "test1@example.com", "test2@example.com" };
        var ccReceipts = new[] { "cc1@example.com" };

        emailHelper.ForType("TestMail")
                  .Map("MachineName", Environment.MachineName)
                  .Map("SmtpServer", "smtp.test.com");

        // Act
        var result = emailHelper.BuildMessage(receipts, ccReceipts);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<CommonMailMessage>(result);
        Assert.Same(receipts, result.Receipts);
        Assert.Same(ccReceipts, result.CcReceipts);
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

        emailHelper.ForType("TestMail")
                  .Map("MachineName", Environment.MachineName)
                  .Map("SmtpServer", "smtp.test.com");

        // Act
        var result = emailHelper.BuildMessage(receipts);

        // Assert
        Assert.NotNull(result);
        Assert.Same(receipts, result.Receipts);
        Assert.Null(result.CcReceipts);
    }

    [Fact]
    public void BuildMessage_WithNullReceipts_ShouldThrowArgumentNullException()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);
        emailHelper.ForType("TestMail");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => emailHelper.BuildMessage(null));
    }

    [Fact]
    public void BuildMessage_WithoutForType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => emailHelper.BuildMessage(["test@example.com"]));
    }

    [Fact]
    public void BuildMessage_WithEmptyReceipts_ShouldThrowArgumentNullException()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);
        emailHelper.ForType("TestMail");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => emailHelper.BuildMessage(new string[0]));
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
        Assert.True(emailHelper.Pipeline.HasEntity("MachineName"));
        Assert.True(emailHelper.Pipeline.HasEntity("SmtpServer"));
        Assert.True(emailHelper.Pipeline.HasEntity("Port"));
    }

    [Fact]
    public void Engine_ShouldBeSetAfterBuildMessage()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration);
        var receipts = new[] { "test@example.com" };

        emailHelper.ForType("TestMail")
                  .Map("MachineName", Environment.MachineName)
                  .Map("SmtpServer", "smtp.test.com");

        // Act
        emailHelper.BuildMessage(receipts);

        // Assert
        Assert.NotNull(emailHelper.Engine);
        Assert.IsType<TemplateEngine>(emailHelper.Engine);
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

        emailHelper.ForType("TestMail");

        // Act
        var result = emailHelper.BuildMessage(receipts);

        // Assert
        Assert.Equal("Test Subject", result.Subject);
        Assert.Equal("Test Body", result.Body);
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
