using System.Globalization;
using Xunit;

namespace Edi.TemplateEmail.Tests;

public class TemplateMailMessageTests
{
    private readonly MailConfiguration _testMailConfiguration;

    public TemplateMailMessageTests()
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

    [Fact]
    public void Constructor_WithNullMailConfig_ShouldSetLoadedToFalse()
    {
        // Act
        var templateMail = new TemplateMailMessage(null, "Welcome");

        // Assert
        Assert.False(templateMail.Loaded);
        Assert.Null(templateMail.Text);
        Assert.False(templateMail.IsHtml);
        Assert.Null(templateMail.Subject);
    }

    [Fact]
    public void Constructor_WithValidMessageType_ShouldLoadCorrectMessage()
    {
        // Act
        var templateMail = new TemplateMailMessage(_testMailConfiguration, "Newsletter");

        // Assert
        Assert.True(templateMail.Loaded);
        Assert.Equal("Monthly Newsletter", templateMail.Subject);
        Assert.Equal("<h2>Newsletter</h2><p>Here's what's new this month.</p>", templateMail.Text);
        Assert.True(templateMail.IsHtml);
    }

    [Fact]
    public void Constructor_WithNonExistentMessageType_ShouldSetLoadedToFalse()
    {
        // Act
        var templateMail = new TemplateMailMessage(_testMailConfiguration, "NonExistent");

        // Assert
        Assert.False(templateMail.Loaded);
        Assert.Null(templateMail.Text);
        Assert.False(templateMail.IsHtml);
        Assert.Null(templateMail.Subject);
    }

    [Fact]
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
            Assert.True(templateMail.Loaded);
            Assert.Equal("Welcome to our service", templateMail.Subject);
            Assert.Equal("<h1>Welcome!</h1><p>Thank you for joining us.</p>", templateMail.Text);
            Assert.True(templateMail.IsHtml);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
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
            Assert.True(templateMail.Loaded);
            Assert.Equal("Welcome to our service", templateMail.Subject);
            Assert.Equal("<h1>Welcome!</h1><p>Thank you for joining us.</p>", templateMail.Text);
            Assert.True(templateMail.IsHtml);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
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
            Assert.True(templateMail.Loaded);
            Assert.Equal("Bienvenue ид notre service", templateMail.Subject);
            Assert.Equal("Bienvenue! Merci de nous avoir rejoint.", templateMail.Text);
            Assert.False(templateMail.IsHtml);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Constructor_WithEmptyMessageType_ShouldSetLoadedToFalse()
    {
        // Act
        var templateMail = new TemplateMailMessage(_testMailConfiguration, "");

        // Assert
        Assert.False(templateMail.Loaded);
        Assert.Null(templateMail.Text);
        Assert.False(templateMail.IsHtml);
        Assert.Null(templateMail.Subject);
    }

    [Fact]
    public void Constructor_WithNullMessageType_ShouldSetLoadedToFalse()
    {
        // Act
        var templateMail = new TemplateMailMessage(_testMailConfiguration, null);

        // Assert
        Assert.False(templateMail.Loaded);
        Assert.Null(templateMail.Text);
        Assert.False(templateMail.IsHtml);
        Assert.Null(templateMail.Subject);
    }

    [Fact]
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
        Assert.False(templateMail.Loaded);
        Assert.Null(templateMail.Text);
        Assert.False(templateMail.IsHtml);
        Assert.Null(templateMail.Subject);
    }

    [Fact]
    public void Constructor_WithNullMailMessages_ShouldThrowException()
    {
        // Arrange
        var configWithNullMessages = new MailConfiguration
        {
            MailMessages = null
        };

        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            new TemplateMailMessage(configWithNullMessages, "Welcome"));
    }

    [Fact]
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
            Assert.True(templateMail.Loaded);
            Assert.Equal("Welcome to our service", templateMail.Subject);
            Assert.True(templateMail.IsHtml);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Constructor_WithMessageHavingNullOrEmptyCulture_ShouldStillBeSelectable()
    {
        // Act
        var templateMail = new TemplateMailMessage(_testMailConfiguration, "Notification");

        // Assert
        Assert.True(templateMail.Loaded);
        Assert.Equal("System Notification", templateMail.Subject);
        Assert.Equal("This is a system notification.", templateMail.Text);
        Assert.False(templateMail.IsHtml);
    }

    [Fact]
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
            Assert.True(templateMail.Loaded);
            Assert.Equal("First Subject", templateMail.Subject);
            Assert.Equal("First Body", templateMail.Text);
            Assert.False(templateMail.IsHtml);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Properties_ShouldHaveCorrectGetters()
    {
        // Arrange
        var templateMail = new TemplateMailMessage(_testMailConfiguration, "Newsletter");

        // Act & Assert
        Assert.NotNull(templateMail.Text);
        Assert.NotNull(templateMail.Subject);
        Assert.True(templateMail.IsHtml);
        Assert.True(templateMail.Loaded);
    }

    [Fact]
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
        Assert.Equal(newText, templateMail.Text);
        Assert.Equal(newSubject, templateMail.Subject);
        Assert.False(templateMail.IsHtml);
        Assert.True(templateMail.Loaded); // Loaded property should be read-only
    }
}