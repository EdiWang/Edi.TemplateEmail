using System.Text;

namespace Edi.TemplateEmail.Tests;

[TestClass]
public class TemplateEngineTests
{
    private TemplateMailMessage _templateMailMessage;
    private TemplatePipeline _pipeline;
    private TemplateEngine _templateEngine;

    [TestInitialize]
    public void TestInitialize()
    {
        // Create a mock mail configuration for testing
        var mailConfig = new MailConfiguration
        {
            MailMessages =
            [
                new MailMessageConfiguration
                {
                    MessageType = "Test",
                    IsHtml = false,
                    MessageSubject = "Test Subject",
                    MessageBody = "Test Body"
                }
            ]
        };

        _templateMailMessage = new TemplateMailMessage(mailConfig, "Test");
        _pipeline = new TemplatePipeline();
        _templateEngine = new TemplateEngine(_templateMailMessage, _pipeline);
    }

    [TestMethod]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Assert
        Assert.AreSame(_pipeline, _templateEngine.Pipeline);
        Assert.AreSame(_templateMailMessage, _templateEngine.TextProvider);
    }

    [TestMethod]
    public void Format_WithSimpleStringReplacement_ShouldReplaceCorrectly()
    {
        // Arrange
        _pipeline.Map("User", "John Doe");
        string template = "Hello {User.Value}!";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual("Hello John Doe!", result);
    }

    [TestMethod]
    public void Format_WithObjectPropertyReplacement_ShouldReplaceCorrectly()
    {
        // Arrange
        var user = new { FirstName = "John", LastName = "Doe" };
        _pipeline.Map("User", user);
        string template = "Hello {User.FirstName} {User.LastName}!";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual("Hello John Doe!", result);
    }

    [TestMethod]
    public void Format_WithMultipleEntities_ShouldReplaceAll()
    {
        // Arrange
        var user = new { Name = "John" };
        var company = new { Name = "ACME Corp" };
        _pipeline.Map("User", user)
                 .Map("Company", company);
        string template = "Dear {User.Name}, welcome to {Company.Name}!";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual("Dear John, welcome to ACME Corp!", result);
    }

    [TestMethod]
    public void Format_WithNonExistentEntity_ShouldReplaceWithEmptyString()
    {
        // Arrange
        string template = "Hello {NonExistent.Property}!";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual("Hello !", result);
    }

    [TestMethod]
    public void Format_WithNonExistentProperty_ShouldReplaceWithEmptyString()
    {
        // Arrange
        var user = new { FirstName = "John" };
        _pipeline.Map("User", user);
        string template = "Hello {User.NonExistentProperty}!";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual("Hello !", result);
    }

    [TestMethod]
    public void Format_WithNoTemplateTokens_ShouldReturnOriginalText()
    {
        // Arrange
        string template = "Hello World! No tokens here.";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual(template, result);
    }

    [TestMethod]
    public void Format_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        string template = "";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void Format_WithSameTokenMultipleTimes_ShouldReplaceAllOccurrences()
    {
        // Arrange
        _pipeline.Map("User", "John");
        string template = "{User.Value} said hello to {User.Value} in the mirror.";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual("John said hello to John in the mirror.", result);
    }

    [TestMethod]
    public void Format_WithComplexObject_ShouldReplaceCorrectly()
    {
        // Arrange
        var user = new TestUser
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Age = 30
        };
        _pipeline.Map("User", user);
        string template = "Name: {User.FirstName} {User.LastName}, Email: {User.Email}, Age: {User.Age}";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual("Name: John Doe, Email: john.doe@example.com, Age: 30", result);
    }

    [TestMethod]
    public void Format_WithMalformedTokens_ShouldNotReplace()
    {
        // Arrange
        _pipeline.Map("User", "John");
        string template = "Hello {User Value} and {User.} and {.Value}!";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual("Hello {User Value} and {User.} and {.Value}!", result);
    }

    [TestMethod]
    public void Format_WithNestedBraces_ShouldOnlyReplaceValidTokens()
    {
        // Arrange
        _pipeline.Map("User", "John");
        string template = "Hello {{User.Value}} and {User.Value}!";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual("Hello {John} and John!", result);
    }

    [TestMethod]
    public void Format_WithNumericProperties_ShouldConvertToString()
    {
        // Arrange
        var stats = new { Count = 42, Average = 3.14 };
        _pipeline.Map("Stats", stats);
        string template = "Count: {Stats.Count}, Average: {Stats.Average}";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual("Count: 42, Average: 3.14", result);
    }

    [TestMethod]
    public void Format_WithBooleanProperties_ShouldConvertToString()
    {
        // Arrange
        var settings = new { IsEnabled = true, IsVisible = false };
        _pipeline.Map("Settings", settings);
        string template = "Enabled: {Settings.IsEnabled}, Visible: {Settings.IsVisible}";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual("Enabled: True, Visible: False", result);
    }

    [TestMethod]
    public void Format_WithNullPropertyValue_ShouldReplaceWithEmptyString()
    {
        // Arrange
        var user = new TestUserWithNullable { FirstName = "John", LastName = null };
        _pipeline.Map("User", user);
        string template = "Hello {User.FirstName} {User.LastName}!";

        // Act
        string result = _templateEngine.Format(() => new StringBuilder(template));

        // Assert
        Assert.AreEqual("Hello John !", result);
    }

    // Helper classes for testing
    private class TestUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
    }

    private class TestUserWithNullable
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}