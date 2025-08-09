using System.IO;
using System.Xml.Serialization;

namespace Edi.TemplateEmail.Tests;

[TestClass]
public class EmailHelperTests
{
    private EmailSettings _testSettings;
    private MailConfiguration _testMailConfiguration;
    private string _testConfigPath;

    [TestInitialize]
    public void TestInitialize()
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

    [TestCleanup]
    public void TestCleanup()
    {
        if (File.Exists(_testConfigPath))
        {
            File.Delete(_testConfigPath);
        }
    }

    [TestMethod]
    public void Constructor_WithConfiguration_ShouldSetProperties()
    {
        // Act
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);

        // Assert
        Assert.AreEqual(_testSettings, emailHelper.Settings);
        Assert.IsNull(emailHelper.Engine);
        Assert.IsNull(emailHelper.Pipeline);
    }

    [TestMethod]
    public void Constructor_WithConfigPath_ShouldSetProperties()
    {
        // Act
        var emailHelper = new EmailHelper(_testConfigPath, _testSettings);

        // Assert
        Assert.AreEqual(_testSettings, emailHelper.Settings);
        Assert.IsNull(emailHelper.Engine);
        Assert.IsNull(emailHelper.Pipeline);
    }

    [TestMethod]
    public void Constructor_WithNullConfigPath_ShouldThrowArgumentNullException()
    {
        // Act
        Assert.ThrowsExactly<ArgumentNullException>(() => new EmailHelper((string)null, _testSettings));
    }

    [TestMethod]
    public void Constructor_WithEmptyConfigPath_ShouldThrowArgumentNullException()
    {
        // Act
        Assert.ThrowsExactly<ArgumentNullException>(() => new EmailHelper(string.Empty, _testSettings));
    }

    [TestMethod]
    public void Constructor_WithWhitespaceConfigPath_ShouldThrowArgumentNullException()
    {
        // Act
        Assert.ThrowsExactly<ArgumentNullException>(() => new EmailHelper("   ", _testSettings));
    }

    [TestMethod]
    public void Constructor_WithNonExistentConfigPath_ShouldThrowFileNotFoundException()
    {
        // Act
        Assert.ThrowsExactly<FileNotFoundException>(() => new EmailHelper("nonexistent.xml", _testSettings));
    }

    [TestMethod]
    public void ForType_ShouldSetMailTypeAndCreatePipeline()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
        const string mailType = "TestMail";

        // Act
        var result = emailHelper.ForType(mailType);

        // Assert
        Assert.AreSame(emailHelper, result); // Should return self for fluent interface
        Assert.IsNotNull(emailHelper.Pipeline);
    }

    [TestMethod]
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
        Assert.AreSame(emailHelper, result); // Should return self for fluent interface
        Assert.IsTrue(emailHelper.Pipeline.HasEntity(name));
        Assert.AreEqual(value, emailHelper.Pipeline[name].Value);
    }

    [TestMethod]
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
        Assert.AreSame(emailHelper, result); // Should return self for fluent interface
        Assert.IsTrue(emailHelper.Pipeline.HasEntity("Name1"));
        Assert.IsTrue(emailHelper.Pipeline.HasEntity("Name2"));
        Assert.IsTrue(emailHelper.Pipeline.HasEntity("Name3"));
        Assert.AreEqual("Value1", emailHelper.Pipeline["Name1"].Value);
        Assert.AreEqual(42, emailHelper.Pipeline["Name2"].Value);
        Assert.AreEqual(true, emailHelper.Pipeline["Name3"].Value);
    }

    [TestMethod]
    public void MapRange_WithNoValues_ShouldReturnSelf()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
        emailHelper.ForType("TestMail");

        // Act
        var result = emailHelper.MapRange();

        // Assert
        Assert.AreSame(emailHelper, result);
    }

    [TestMethod]
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
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(CommonMailMessage));
        Assert.AreSame(receipts, result.Receipts);
        Assert.AreSame(ccReceipts, result.CcReceipts);
        Assert.AreSame(_testSettings, result.Settings);
        Assert.IsNotNull(result.Subject);
        Assert.IsNotNull(result.Body);
        Assert.IsTrue(result.BodyIsHtml);
    }

    [TestMethod]
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
        Assert.IsNotNull(result);
        Assert.AreSame(receipts, result.Receipts);
        Assert.IsNull(result.CcReceipts);
        Assert.AreSame(_testSettings, result.Settings);
    }

    [TestMethod]
    public void BuildMessage_WithNullReceipts_ShouldThrowArgumentNullException()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
        emailHelper.ForType("TestMail");

        // Act
        Assert.ThrowsExactly<ArgumentNullException>(() => emailHelper.BuildMessage(null));
    }

    [TestMethod]
    public void BuildMessage_WithEmptyReceipts_ShouldThrowArgumentNullException()
    {
        // Arrange
        var emailHelper = new EmailHelper(_testMailConfiguration, _testSettings);
        emailHelper.ForType("TestMail");

        // Act
        Assert.ThrowsExactly<ArgumentNullException>(() => emailHelper.BuildMessage(new string[0]));
    }

    [TestMethod]
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
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(CommonMailMessage));
        Assert.IsTrue(emailHelper.Pipeline.HasEntity("MachineName"));
        Assert.IsTrue(emailHelper.Pipeline.HasEntity("SmtpServer"));
        Assert.IsTrue(emailHelper.Pipeline.HasEntity("Port"));
    }

    [TestMethod]
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
        Assert.IsNotNull(emailHelper.Engine);
        Assert.IsInstanceOfType(emailHelper.Engine, typeof(TemplateEngine));
    }

    [TestMethod]
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
        Assert.AreEqual("Test Subject", result.Subject);
        Assert.AreEqual("Test Body", result.Body);
    }

    [TestMethod]
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
        Assert.IsTrue(htmlResult.BodyIsHtml);
        Assert.IsFalse(textResult.BodyIsHtml);
    }
}