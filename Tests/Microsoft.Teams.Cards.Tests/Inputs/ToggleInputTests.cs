using System.Text.Json;
namespace Microsoft.Teams.Cards;


public class ToggleInputTests
{
    [Fact]
    public void validateToggleInputDefault()
    {
       var toggleInput = new ToggleInput("default");
       
        var json = JsonSerializer.Serialize(toggleInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Toggle",
            title = "default"
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void validateToggleInputBoolValue(bool value, string expectedValue)
    {
        var toggleInput = new ToggleInput("default", value);

        var json = JsonSerializer.Serialize(toggleInput, new JsonSerializerOptions()
        {
           DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Toggle",
            title = "default",
            value = expectedValue,
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }
    [Theory]
    [InlineData("True")]
    [InlineData("False" )]
    [InlineData("ValueString" )]
    public void validateToggleInputStringValue(string value)
    {
        var toggleInput = new ToggleInput("default", value);

        var json = JsonSerializer.Serialize(toggleInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Toggle",
            title = "default",
            value = value,
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }


    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void validateWithValueBool(bool value, string expectedValue)
    {
        var toggleInput = new ToggleInput("default");
        var withValueToggleInput =  toggleInput.WithValue(value);

        var actualJson = JsonSerializer.Serialize(withValueToggleInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Toggle",
            title = "default",
            value = expectedValue,
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, actualJson);
    }

    [Theory]
    [InlineData("True")]
    [InlineData("False")]
    [InlineData("ValueString")]
    public void validateWithValueString(string value)
    {
        var toggleInput = new ToggleInput("default");
        var withValueToggleInput = toggleInput.WithValue(value);

        var actualJson = JsonSerializer.Serialize(withValueToggleInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Toggle",
            title = "default",
            value = value,
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, actualJson);
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void validateWithValueOffBool(bool value, string expectedValue)
    {
        var toggleInput = new ToggleInput("default");
        var withValueToggleInput = toggleInput.WithValueOff(value);

        var actualJson = JsonSerializer.Serialize(withValueToggleInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Toggle",
            title = "default",
            valueOff = expectedValue,
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, actualJson);
    }

    [Theory]
    [InlineData("True")]
    [InlineData("False")]
    [InlineData("ValueString")]
    public void validateWithValueOffString(string value)
    {
        var toggleInput = new ToggleInput("default");
        var withValueToggleInput = toggleInput.WithValueOff(value);

        var actualJson = JsonSerializer.Serialize(withValueToggleInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Toggle",
            title = "default",
            valueOff = value,
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, actualJson);
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void validateWithValueOnBool(bool value, string expectedValue)
    {
        var toggleInput = new ToggleInput("default");
        var withValueToggleInput = toggleInput.WithValueOn(value);

        var actualJson = JsonSerializer.Serialize(withValueToggleInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Toggle",
            title = "default",
            valueOn = expectedValue,
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, actualJson);
    }

    [Theory]
    [InlineData("True")]
    [InlineData("False")]
    [InlineData("ValueString")]
    public void validateWithValueOnString(string value)
    {
        var toggleInput = new ToggleInput("default");
        var withValueToggleInput = toggleInput.WithValueOn(value);

        var actualJson = JsonSerializer.Serialize(withValueToggleInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Toggle",
            title = "default",
            valueOn = value,
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, actualJson);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void validateWithWrapBool(bool value )
    {
        var toggleInput = new ToggleInput("default");
        var withValueToggleInput = toggleInput.WithWrap(value);

        var actualJson = JsonSerializer.Serialize(withValueToggleInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Toggle",
            title = "default",
            wrap = value,
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, actualJson);
    }

    [Fact]
    public void validateWithWrapDefault()
    {
        var toggleInput = new ToggleInput("default");
        var withValueToggleInput = toggleInput.WithWrap();

        var actualJson = JsonSerializer.Serialize(withValueToggleInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var exectedObject = new
        {
            type = "Input.Toggle",
            title = "default",
            wrap = true,
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, actualJson);
    }
}