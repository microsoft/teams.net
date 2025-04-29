using System.Text.Json;
namespace Microsoft.Teams.Cards.Tests.Inputs;


public class TimeInputTests
{
    [Fact]
    public void validateTimeInputDefault()
    {
        var TimeInput = new TimeInput("default");

        var json = JsonSerializer.Serialize(TimeInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Time",
            value = "default"
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("22")]
    [InlineData("22:55")]
    public void validateTimeInput(string value)
    {
        var timeInput = new TimeInput(value);

        var json = JsonSerializer.Serialize(timeInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Time",
            value
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Theory]
    [InlineData("string value")]
    [InlineData("12:12")]
    public void validateWithMaxString(string value)
    {
        var timeInput = new TimeInput("default");
        var withMaxTimeInput = timeInput.WithMax(value);

        var actualJson = JsonSerializer.Serialize(withMaxTimeInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Time",
            max = value,
            value = "default",
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, actualJson);
    }

    [Theory]
    [InlineData("2025-02-02 12:12:12", "12:12 PM")]
    [InlineData("2025-02-02 12:48:12", "12:48 PM")]
    // TODO Validate propert handling of incorrect date string [InlineData("2025-02-02 24:48:12", "")] 
    public void validateWithMaxDateTime(DateTime value, string expectedMax)
    {
        var timeInput = new TimeInput("default");
        var withMaxTimeInput = timeInput.WithMax(value);

        var actualJson = JsonSerializer.Serialize(withMaxTimeInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Time",
            max = expectedMax,
            value = "default",
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, actualJson);
    }

    [Theory]
    [InlineData("2025-02-02 12:12:12", "12:12 PM")]
    [InlineData("2025-02-02 12:48:12", "12:48 PM")]
    // TODO Validate propert handling of incorrect date string [InlineData("2025-02-02 24:48:12", "")] 
    public void validateWithMinDateTime(DateTime value, string expectedMin)
    {
        var timeInput = new TimeInput("default");
        var withMinTimeInput = timeInput.WithMin(value);

        var actualJson = JsonSerializer.Serialize(withMinTimeInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Time",
            min = expectedMin,
            value = "default",
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, actualJson);
    }

    [Fact]
    public void validateWithPlaceholder()
    {
        var timeInput = new TimeInput("default");
        var withPlaceholderTimeInput = timeInput.WithPlaceholder("Display placeholder");

        var actualJson = JsonSerializer.Serialize(withPlaceholderTimeInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Time",
            placeholder = "Display placeholder",
            value = "default",
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, actualJson);
    }

    [Fact]
    public void validateWitValue()
    {
        var timeInput = new TimeInput("default");
        var withValueTimeInput = timeInput.WithValue("valuestring");

        var actualJson = JsonSerializer.Serialize(withValueTimeInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Time",
            value = "valuestring",
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, actualJson);
    }
}