using System.Reflection;
using Xunit;

namespace Edi.TemplateEmail.Tests;

public class PipelineItemTests
{
    [Fact]
    public void GetValue_WithScalarValueToken_ShouldUseToString()
    {
        // Arrange
        object[] values =
        [
            new DateTime(2026, 11, 3, 12, 34, 56),
            new DateOnly(2026, 11, 3),
            new TimeOnly(12, 34, 56),
            42, 12.34m, true, Guid.Empty, DayOfWeek.Monday
        ];

        foreach (var value in values)
        {
            var item = new PipelineItem { Value = value };

            // Act
            string result = item.GetValue("Value");

            // Assert
            Assert.Equal(value.ToString(), result);
        }
    }

    [Fact]
    public void GetValue_WithValueProperty_ShouldUseWholeObjectToString()
    {
        // Arrange
        var item = new PipelineItem { Value = new ValueModel() };

        // Act
        string result = item.GetValue("Value");

        // Assert
        Assert.Equal("Whole object", result);
    }

    [Fact]
    public void GetValue_WithListIndexer_ShouldReturnEmptyString()
    {
        // Arrange
        var item = new PipelineItem { Value = new List<string> { "First item" } };

        // Act
        string result = item.GetValue("Item");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetValue_WithOverloadedIndexers_ShouldReturnEmptyString()
    {
        // Arrange
        var item = new PipelineItem { Value = new IndexedModel() };

        // Act
        string result = item.GetValue("Item");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetValue_WithPrivateGetter_ShouldReturnEmptyString()
    {
        // Arrange
        var item = new PipelineItem { Value = new PropertyModel() };

        // Act
        string result = item.GetValue("PrivateGetter");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetValue_WithWriteOnlyProperty_ShouldReturnEmptyString()
    {
        // Arrange
        var item = new PipelineItem { Value = new PropertyModel() };

        // Act
        string result = item.GetValue("WriteOnly");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetValue_WithStaticProperty_ShouldReturnEmptyString()
    {
        // Arrange
        var item = new PipelineItem { Value = new PropertyModel() };

        // Act
        string result = item.GetValue("StaticProperty");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetValue_WithThrowingGetter_ShouldPropagateException()
    {
        // Arrange
        var item = new PipelineItem { Value = new PropertyModel() };

        // Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() => item.GetValue("Throwing"));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("Getter failed.", exception.InnerException.Message);
    }

    [Fact]
    public void GetValue_WithoutPublicProperties_ShouldKeepToStringFallback()
    {
        // Arrange
        var item = new PipelineItem { Value = new TextModel() };

        // Act
        string result = item.GetValue("AnyProperty");

        // Assert
        Assert.Equal("Formatted value", result);
    }

    private class ValueModel
    {
        public string Value => "Property value";

        public override string ToString() => "Whole object";
    }

    private class IndexedModel
    {
        public string this[int index] => throw new InvalidOperationException();

        public string this[string index] => throw new InvalidOperationException();
    }

    private class PropertyModel
    {
        public string PrivateGetter { private get; set; } = "Hidden value";

        public string WriteOnly { set { } }

        public static string StaticProperty => "Static value";

        public string Throwing => throw new InvalidOperationException("Getter failed.");
    }

    private class TextModel
    {
        public override string ToString() => "Formatted value";
    }
}
