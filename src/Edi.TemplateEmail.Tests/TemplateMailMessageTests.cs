using System.Globalization;

namespace Edi.TemplateEmail.Tests;

[TestClass]
public class TemplateMailMessageTests
{
    private MailConfiguration _testMailConfiguration;

    [TestInitialize]
    public void TestInitialize()
    {
        _testMailConfiguration = new MailConfiguration
        {
            MailMessages =
            [
                new MailMessageConfiguration
                {
                    MessageType = "Welcome",
                    IsHtml = true,
                    MessageSubject = "Welcome to our service",
                    MessageBody = "<h1>Welcome!</h1><p>Thank you for joining us.</p>",
                    MessageCulture = "en-US"
                },
                new MailMessageConfiguration
                {
                    MessageType = "Welcome",
                    IsHtml = false,
                    MessageSubject = "Bienvenue ид notre service",
                    MessageBody = "Bienvenue! Merci de nous avoir rejoint.",
                    MessageCulture = "fr-FR"
                },
                new MailMessageConfiguration
                {
                    MessageType = "Newsletter",
                    IsHtml = true,
                    MessageSubject = "Monthly Newsletter",
                    MessageBody = "<h2>Newsletter</h2><p>Here's what's new this month.</p>",
                    MessageCulture = null
                },
                new MailMessageConfiguration
                {
                    MessageType = "Notification",
                    IsHtml = false,
                    MessageSubject = "System Notification",
                    MessageBody = "This is a system notification.",
                    MessageCulture = ""
                }
            ]
        };
    }

    [TestMethod]
    public void Constructor_WithNullMailConfig_ShouldSetLoadedToFalse()
    {
        // Act
        var templateMail = new TemplateMailMessage(null, "Welcome");

        // Assert
        Assert.IsFalse(templateMail.Loaded);
        Assert.IsNull(templateMail.Text);
        Assert.IsFalse(templateMail.IsHtml);
        Assert.IsNull(templateMail.Subject);
    }

    [TestMethod]
    public void Constructor_WithValidMessageType_ShouldLoadCorrectMessage()
    {
        // Act
        var templateMail = new TemplateMailMessage(_testMailConfiguration, "Newsletter");

        // Assert
        Assert.IsTrue(templateMail.Loaded);
        Assert.AreEqual("Monthly Newsletter", templateMail.Subject);
        Assert.AreEqual("<h2>Newsletter</h2><p>Here's what's new this month.</p>", templateMail.Text);
        Assert.IsTrue(templateMail.IsHtml);
    }

    [TestMethod]
    public void Constructor_WithNonExistentMessageType_ShouldSetLoadedToFalse()
    {
        // Act
        var templateMail = new TemplateMailMessage(_testMailConfiguration, "NonExistent");

        // Assert
        Assert.IsFalse(templateMail.Loaded);
        Assert.IsNull(templateMail.Text);
        Assert.IsFalse(templateMail.IsHtml);
        Assert.IsNull(templateMail.Subject);
    }

    [TestMethod]
    public void Constructor_WithMatchingCulture_ShouldSelectCultureSpecificMessage()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            // Act
            var templateMail = new TemplateMailMessage(_testMailConfiguration, "Welcome");

            // Assert
            Assert.IsTrue(templateMail.Loaded);
            Assert.AreEqual("Welcome to our service", templateMail.Subject);
            Assert.AreEqual("<h1>Welcome!</h1><p>Thank you for joining us.</p>", templateMail.Text);
            Assert.IsTrue(templateMail.IsHtml);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void Constructor_WithNonMatchingCulture_ShouldSelectFirstMessage()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            // Act
            var templateMail = new TemplateMailMessage(_testMailConfiguration, "Welcome");

            // Assert
            Assert.IsTrue(templateMail.Loaded);
            Assert.AreEqual("Welcome to our service", templateMail.Subject);
            Assert.AreEqual("<h1>Welcome!</h1><p>Thank you for joining us.</p>", templateMail.Text);
            Assert.IsTrue(templateMail.IsHtml);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void Constructor_WithFrenchCulture_ShouldSelectFrenchMessage()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            // Act
            var templateMail = new TemplateMailMessage(_testMailConfiguration, "Welcome");

            // Assert
            Assert.IsTrue(templateMail.Loaded);
            Assert.AreEqual("Bienvenue ид notre service", templateMail.Subject);
            Assert.AreEqual("Bienvenue! Merci de nous avoir rejoint.", templateMail.Text);
            Assert.IsFalse(templateMail.IsHtml);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void Constructor_WithEmptyMessageType_ShouldSetLoadedToFalse()
    {
        // Act
        var templateMail = new TemplateMailMessage(_testMailConfiguration, "");

        // Assert
        Assert.IsFalse(templateMail.Loaded);
        Assert.IsNull(templateMail.Text);
        Assert.IsFalse(templateMail.IsHtml);
        Assert.IsNull(templateMail.Subject);
    }

    [TestMethod]
    public void Constructor_WithNullMessageType_ShouldSetLoadedToFalse()
    {
        // Act
        var templateMail = new TemplateMailMessage(_testMailConfiguration, null);

        // Assert
        Assert.IsFalse(templateMail.Loaded);
        Assert.IsNull(templateMail.Text);
        Assert.IsFalse(templateMail.IsHtml);
        Assert.IsNull(templateMail.Subject);
    }

    [TestMethod]
    public void Constructor_WithEmptyMailMessages_ShouldSetLoadedToFalse()
    {
        // Arrange
        var emptyConfig = new MailConfiguration
        {
            MailMessages = []
        };

        // Act
        var templateMail = new TemplateMailMessage(emptyConfig, "Welcome");

        // Assert
        Assert.IsFalse(templateMail.Loaded);
        Assert.IsNull(templateMail.Text);
        Assert.IsFalse(templateMail.IsHtml);
        Assert.IsNull(templateMail.Subject);
    }

    [TestMethod]
    public void Constructor_WithNullMailMessages_ShouldThrowException()
    {
        // Arrange
        var configWithNullMessages = new MailConfiguration
        {
            MailMessages = null
        };

        // Act & Assert
        Assert.ThrowsExactly<NullReferenceException>(() => 
            new TemplateMailMessage(configWithNullMessages, "Welcome"));
    }

    [TestMethod]
    public void Constructor_WithCaseInsensitiveCultureMatch_ShouldSelectCorrectMessage()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("EN-us");

            // Act
            var templateMail = new TemplateMailMessage(_testMailConfiguration, "Welcome");

            // Assert
            Assert.IsTrue(templateMail.Loaded);
            Assert.AreEqual("Welcome to our service", templateMail.Subject);
            Assert.IsTrue(templateMail.IsHtml);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void Constructor_WithMessageHavingNullOrEmptyCulture_ShouldStillBeSelectable()
    {
        // Act
        var templateMail = new TemplateMailMessage(_testMailConfiguration, "Notification");

        // Assert
        Assert.IsTrue(templateMail.Loaded);
        Assert.AreEqual("System Notification", templateMail.Subject);
        Assert.AreEqual("This is a system notification.", templateMail.Text);
        Assert.IsFalse(templateMail.IsHtml);
    }

    [TestMethod]
    public void Constructor_WithMultipleMessagesOfSameTypeAndNoCultureMatch_ShouldSelectFirst()
    {
        // Arrange
        var configWithMultiple = new MailConfiguration
        {
            MailMessages =
            [
                new MailMessageConfiguration
                {
                    MessageType = "Test",
                    IsHtml = false,
                    MessageSubject = "First Subject",
                    MessageBody = "First Body",
                    MessageCulture = "es-ES"
                },
                new MailMessageConfiguration
                {
                    MessageType = "Test",
                    IsHtml = true,
                    MessageSubject = "Second Subject",
                    MessageBody = "Second Body",
                    MessageCulture = "it-IT"
                }
            ]
        };

        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            // Act
            var templateMail = new TemplateMailMessage(configWithMultiple, "Test");

            // Assert
            Assert.IsTrue(templateMail.Loaded);
            Assert.AreEqual("First Subject", templateMail.Subject);
            Assert.AreEqual("First Body", templateMail.Text);
            Assert.IsFalse(templateMail.IsHtml);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void Properties_ShouldHaveCorrectGetters()
    {
        // Arrange
        var templateMail = new TemplateMailMessage(_testMailConfiguration, "Newsletter");

        // Act & Assert
        Assert.IsNotNull(templateMail.Text);
        Assert.IsNotNull(templateMail.Subject);
        Assert.IsTrue(templateMail.IsHtml);
        Assert.IsTrue(templateMail.Loaded);
    }

    [TestMethod]
    public void Properties_ShouldAllowSetting()
    {
        // Arrange
        var templateMail = new TemplateMailMessage(_testMailConfiguration, "Newsletter");
        const string newText = "New text content";
        const string newSubject = "New subject";

        // Act
        templateMail.Text = newText;
        templateMail.Subject = newSubject;
        templateMail.IsHtml = false;

        // Assert
        Assert.AreEqual(newText, templateMail.Text);
        Assert.AreEqual(newSubject, templateMail.Subject);
        Assert.IsFalse(templateMail.IsHtml);
        Assert.IsTrue(templateMail.Loaded); // Loaded property should be read-only
    }
}