
using System.Text.Json;

using Microsoft.Teams.Common.Json;

namespace Microsoft.Teams.Common.Tests.Json;

[TrueTypeJson<IValidateEvent>]
public interface IValidateEvent
{
    public string Id { get; }
    public string Type { get; }
    public object? Body { get; }
    public DateTime SentAt { get; }
}

public class ValidateEvent : IValidateEvent
{
    public required string Id { get; set; }
    public string Type { get; } = "test";
    public object? Body { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

public class TrueTypeJsonAttributeTests
{
    [Fact]
    public void TrueTypeJsonAttribute_SetsConverterType()
    {
        var attr = new TrueTypeJsonAttribute<string>();
        Assert.Equal(typeof(TrueTypeJsonConverter<string>), attr.ConverterType);
    }

    [Fact]
    public void TrueTypeJsonAttribute_SetsConverterTypeObject()
    {
        var attr = new TrueTypeJsonAttribute<IValidateEvent>();
        Assert.Equal(typeof(TrueTypeJsonConverter<IValidateEvent>), attr.ConverterType);
    }

    [Fact]
    public void TrueTypeJsonConverter_Serialize()
    {
        // Arrange   
        var validateEvent = new ValidateEvent
        {
            Id = "bodyGuid"
        };
        Assert.True(validateEvent is ValidateEvent);

        // Act
        var json = JsonSerializer.Serialize(validateEvent);
        // Assert
        Assert.Contains("\"Id\":\"bodyGuid\"", json);
        Assert.Contains("\"Type\":\"test\"", json);
        Assert.Contains("\"SentAt\":\"", json);

    }

    [Fact]
    public void TrueTypeJsonConverter_Deserialize()
    {
        // Arrange   
        var validateEvent = new ValidateEvent()
        {
            Id = "guid",
            SentAt = DateTime.UtcNow
        };
        var json = JsonSerializer.Serialize<IValidateEvent>(validateEvent);

        // Act
        var ex = Assert.Throws<System.NotImplementedException>(() => JsonSerializer.Deserialize<IValidateEvent>(json));
        // Assert
        var expectedSubmitException = "The method or operation is not implemented.";
        Assert.Equal(expectedSubmitException, ex.Message);
    }
}