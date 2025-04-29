
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.Teams.Cards.Tests.Inputs;
public class TextInputTests
{
    [Fact]
    public void validateTextInputDefault()
    {
        var textInput = new TextInput(null);

        var json = JsonSerializer.Serialize(textInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Text",
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Theory]
    [InlineData("True")]
    [InlineData("False")]
    [InlineData("ValueString")]
    public void validateTextInputValueString(string value)
    {
        var textInput = new TextInput(value);

        var json = JsonSerializer.Serialize(textInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Text",
            value
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateTextInputWithMultiLineString()
    {
        var textInput = new TextInput("default value");
        textInput.WithMultiLine();

        var json = JsonSerializer.Serialize(textInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Text",
            isMultiline = true,
            value = "default value"
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(0)]
    [InlineData(-1)] // TODO Validate this scenario should be handled
    public void validateTextInputWithMaxLength(int value)
    {
        var textInput = new TextInput("default value");
        textInput.WithMaxLength(value);

        var json = JsonSerializer.Serialize(textInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Text",
            maxLength = value,
            value = "default value"
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateTextInputWithPlaceholderString()
    {
        var textInput = new TextInput("default value");
        textInput.WithPlaceholder("Show placeholder value");

        var json = JsonSerializer.Serialize(textInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Text",
            placeholder = "Show placeholder value",
            value = "default value"
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateTextInputWithRegexString()
    {
        var textInput = new TextInput("default value");
        textInput.WithRegex("RegExString");

        var json = JsonSerializer.Serialize(textInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Text",
            regex = "RegExString",
            value = "default value"
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateTextInputWithRegex()
    {
        var textInput = new TextInput("default value");
        Regex regex = new Regex("%s");
        textInput.WithRegex(regex);

        var json = JsonSerializer.Serialize(textInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Text",
            regex = "%s",
            value = "default value"
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }


    [Fact]
    public void validateTextInputWithStyle()
    {
        var textInput = new TextInput("default value");
        TextInputStyle style = new TextInputStyle("somestyle");
        textInput.WithStyle(style);

        var json = JsonSerializer.Serialize(textInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Text",
            value = "default value",
            style = "somestyle"

        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateTextInputWithInlineAction()
    {
        var textInput = new TextInput("default value");
        CardType cardType = new CardType("test");
        SelectAction action = new SelectAction(cardType);
        textInput.WithInlineAction(action);

        var json = JsonSerializer.Serialize(textInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Text",
            value = "default value",
            inlineAction = action
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateTextInputWithValue()
    {
        var textInput = new TextInput("default value");
       
        textInput.WithValue("new value");

        var json = JsonSerializer.Serialize(textInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Text",
            value = "new value",      
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }
}
