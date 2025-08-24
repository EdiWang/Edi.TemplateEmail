using System.Xml.Serialization;
using Xunit;

namespace Edi.TemplateEmail.Tests;

public class EmailHelperTests : IDisposable
{
    private readonly EmailSettings _testSettings;
    private readonly MailConfiguration _testMailConfiguration;
    private readonly string _testConfigPath;

    public EmailHelperTests()
    {
        _testSettings = new EmailSettings
        {
            SenderName = "test@example.com",
            EmailDisplayName = "Test Sender",
            SmtpSettings = new SmtpSettings("smtp.test.com", "testuser", "testpass", 587)
            {
                EnableTls = true
            }
        };

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
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);

        // Assert
        Assert.Equal(_testSettings, emailHelper.Settings);
        Assert.Null(emailHelper.Engine);
        Assert.Null(emailHelper.Pipeline);
    }

    [Fact]
    public void Constructor_WithConfigPath_ShouldSetProperties()
    {
        // Act
        var emailHelper = new EmailHelper(_testConfigPath, _testSettings);

        // Assert
        Assert.Equal(_testSettings, emailHelper.Settings);
        Assert.Null(emailHelper.Engine);
        Assert.Null(emailHelper.Pipeline);
    }

    [Fact]
    public void Constructor_WithNullConfigPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailHelper((string)null, _testSettings));
    }

    [Fact]
    public void Constructor_WithEmptyConfigPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailHelper(string.Empty, _testSettings));
    }

    [Fact]
    public void Constructor_WithWhitespaceConfigPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailHelper("   ", _testSettings));
    }

    [Fact]
    public void Constructor_WithNonExistentConfigPath_ShouldThrowFileNotFoundException()
    {
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => new EmailHelper("nonexistent.xml", _testSettings));
    }

    [Fact]
    public void ForType_ShouldSetMailTypeAndCreatePipeline()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
        const string mailType = "TestMail";

        // Act
        var result = emailHelper.ForType(mailType);

        // Assert
        Assert.Same(emailHelper, result); // Should return self for fluent interface
        Assert.NotNull(emailHelper.Pipeline);
    }

    [Fact]
    public void Map_ShouldAddValueToPipeline()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
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
    public void MapRange_ShouldAddMultipleValuesToPipeline()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
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
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
        emailHelper.ForType("TestMail");

        // Act
        var result = emailHelper.MapRange();

        // Assert
        Assert.Same(emailHelper, result);
    }

    [Fact]
    public void BuildMessage_WithValidReceipts_ShouldReturnCommonMailMessage()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
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
        Assert.Same(_testSettings, result.Settings);
        Assert.NotNull(result.Subject);
        Assert.NotNull(result.Body);
        Assert.True(result.BodyIsHtml);
    }

    [Fact]
    public void BuildMessage_WithOnlyReceipts_ShouldReturnCommonMailMessage()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
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
        Assert.Same(_testSettings, result.Settings);
    }

    [Fact]
    public void BuildMessage_WithNullReceipts_ShouldThrowArgumentNullException()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
        emailHelper.ForType("TestMail");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => emailHelper.BuildMessage(null));
    }

    [Fact]
    public void BuildMessage_WithEmptyReceipts_ShouldThrowArgumentNullException()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
        emailHelper.ForType("TestMail");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => emailHelper.BuildMessage(new string[0]));
    }

    [Fact]
    public void FluentInterface_ShouldWorkCorrectly()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
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
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
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

        var emailHelper = new EmailHelper(configWithWhitespace, _testSettings);
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

        var emailHelperHtml = new EmailHelper(htmlConfig, _testSettings);
        var emailHelperText = new EmailHelper(textConfig, _testSettings);
        var receipts = new[] { "test@example.com" };

        // Act
        var htmlResult = emailHelperHtml.ForType("HtmlMail").BuildMessage(receipts);
        var textResult = emailHelperText.ForType("TextMail").BuildMessage(receipts);

        // Assert
        Assert.True(htmlResult.BodyIsHtml);
        Assert.False(textResult.BodyIsHtml);
    }
}