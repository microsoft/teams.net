
using System.Text.Json;

namespace Microsoft.Teams.Cards.Tests.Inputs;
public class NumberInputTests
{
    [Fact]
    public void validateNumberInputDefault()
    {
        var numberInput = new NumberInput(null);

        var json = JsonSerializer.Serialize(numberInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Number",
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Theory]
    [InlineData(1.45)]
    [InlineData(-6)]
    [InlineData(12343434342222)]
    public void validateNumberInputValueDouble(double value)
    {
        var numberInput = new NumberInput(value);

        var json = JsonSerializer.Serialize(numberInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Number",
            value
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Theory]
    [InlineData(1.45)]
    [InlineData(-6)]
    [InlineData(12343434342222)]
    public void validateNumberInputWithMax(double withMaxValue)
    {
        var numberInput = new NumberInput(250);
        numberInput.WithMax(withMaxValue);

        var json = JsonSerializer.Serialize(numberInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Number",
            max = withMaxValue,
            value = 250
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Theory]
    [InlineData(1.45)]
    [InlineData(-6)]
    [InlineData(12343434342222)]
    public void validateNumberInputWithMin(double withMinValue)
    {
        var numberInput = new NumberInput(250);
        numberInput.WithMin(withMinValue);

        var json = JsonSerializer.Serialize(numberInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Number",
            min = withMinValue,
            value = 250
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateNumberInputWithPlaceholderString()
    {
        var numberInput = new NumberInput(200);
        numberInput.WithPlaceholder("Show placeholder value");

        var json = JsonSerializer.Serialize(numberInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Number",
            placeholder = "Show placeholder value",
            value = 200
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }


    [Fact]
    public void validateNumberInputWithValue()
    {
        var numberInput = new NumberInput(200);

        numberInput.WithValue(50);

        var json = JsonSerializer.Serialize(numberInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Number",
            value = 50,
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }
}
