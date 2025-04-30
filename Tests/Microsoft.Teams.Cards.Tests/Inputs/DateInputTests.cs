
using System.Globalization;
using System.Text.Json;

namespace Microsoft.Teams.Cards.Tests.Inputs;
public class DateInputTests
{
    [Fact]
    public void validateInputElementDefault()
    {
        var dateInput = new DateInput();

        var json = JsonSerializer.Serialize(dateInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Date",
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Theory]
    [InlineData("1.45")]
    [InlineData("-6")]
    [InlineData("invalid string")]
    public void validateInputElementString(string value)
    {
        var dateInput = new DateInput(value);

        var json = JsonSerializer.Serialize(dateInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Date",
            value
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }


    [Theory]
    [InlineData("2025-12-12 03:04:33", "12/12/2025")]
    // TODO handle error case [InlineData("2025-12-12 24:04:33", "12/12/2025")]
    public void validateInputElementDateTime(DateTime value, string expectedValue )
    {
        var dateInput = new DateInput(value);

        var json = JsonSerializer.Serialize(dateInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Date",
            value =expectedValue
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]  
    public void validateInputElementWithMax()
    {
        var dateInput = new DateInput();
        dateInput.WithMax("2025-12-12 03:04:33");

        var json = JsonSerializer.Serialize(dateInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Date",
            max = "2025-12-12 03:04:33"
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateInputElementWithMin()
    {
        var dateInput = new DateInput();
        dateInput.WithMin("2025-12-12 03:04:33");

        var json = JsonSerializer.Serialize(dateInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Date",
            min = "2025-12-12 03:04:33"
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateDateInputWithPlaceholderString()
    {
        var dateInput = new DateInput();
        dateInput.WithPlaceholder("Show placeholder value");

        var json = JsonSerializer.Serialize(dateInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Date",
            placeholder = "Show placeholder value",
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }


    [Fact]
    public void validateDateInputWithValue()
    {
        var dateInput = new DateInput();

        dateInput.WithValue("12:12");

        var json = JsonSerializer.Serialize(dateInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Date",
            value = "12:12",
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateDateInputWithValueDateTime()
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("nl-NL");
        var dateInput = new DateInput();
       
        dateInput.WithValue(DateTime.Parse("2025-04-30 12:22:55"));

        var json = JsonSerializer.Serialize(dateInput, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.Date",
            value = "30-04-2025",
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

}
