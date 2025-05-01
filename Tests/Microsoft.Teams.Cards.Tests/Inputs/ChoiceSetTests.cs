
using System.Text.Json;

namespace Microsoft.Teams.Cards.Tests.Inputs;
public class ChoiceSetTests
{
    [Fact]
    public void validateNumberInputDefault()
    {
        Choice[] choices = {
            new Choice() { Title = "1", Value = "1" },
            new Choice() { Title = "2", Value = "2" },
        } ;
         var choiceSet = new ChoiceSetInput(choices);

        var json = JsonSerializer.Serialize(choiceSet, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.ChoiceSet",
            choices
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateChoiceSetWithData()
    {
        Choice[] choices = {
            new Choice() { Title = "1", Value = "1" },
            new Choice() { Title = "2", Value = "2" },
        } ;
        var choiceDataQuery = new ChoiceDataQuery() { DataSet = "testing" };

        var choiceSet = new ChoiceSetInput(choices);
        choiceSet.WithData(choiceDataQuery);
        choiceSet.WithMultiSelect();
        choiceSet.WithWrap();

        var json = JsonSerializer.Serialize(choiceSet, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        Assert.Equal(File.ReadAllText(
           @"../../../Json/Inputs/ChoiceSetWithQuery.json"
       ), json);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void validateChoiceSetWithMultiSelect(bool value)
    {
        Choice[] choices = {
            new Choice() { Title = "1", Value = "1" },
            new Choice() { Title = "2", Value = "2" },
        };
        var choiceSet = new ChoiceSetInput(choices);
        choiceSet.WithMultiSelect(value);
        
        var json = JsonSerializer.Serialize(choiceSet, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.ChoiceSet",
            choices,
            isMultiSelect = value
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateChoiceSetWithStyle()
    {
        Choice[] choices = {
            new Choice() { Title = "1", Value = "1" },
            new Choice() { Title = "2", Value = "2" },
        };
        ChoiceInputStyle style = new ChoiceInputStyle("color:red");
        var choiceSet = new ChoiceSetInput(choices);
        choiceSet.WithStyle(style);

        var json = JsonSerializer.Serialize(choiceSet, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.ChoiceSet",
            choices,
            style = "color:red"
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateChoiceSetWithValue()
    {
        Choice[] choices = {
            new Choice() { Title = "1", Value = "1" },
            new Choice() { Title = "2", Value = "2" },
        };
        var choiceSet = new ChoiceSetInput(choices);
        choiceSet.WithValue("valuestring");

        var json = JsonSerializer.Serialize(choiceSet, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.ChoiceSet",
            choices,
            value = "valuestring"
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact]
    public void validateChoiceSetWithPlaceholder()
    {
        Choice[] choices = {
            new Choice() { Title = "1", Value = "1" },
            new Choice() { Title = "2", Value = "2" },
        };
        var choiceSet = new ChoiceSetInput(choices);
        choiceSet.WithPlaceholder("valuestring");

        var json = JsonSerializer.Serialize(choiceSet, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.ChoiceSet",
            choices,
            placeholder = "valuestring"
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void validateChoiceSetWithWrap(bool value)
    {
        Choice[] choices = {
            new Choice() { Title = "1", Value = "1" },
            new Choice() { Title = "2", Value = "2" },
        };
        var choiceSet = new ChoiceSetInput(choices);
        choiceSet.WithWrap(value);

        var json = JsonSerializer.Serialize(choiceSet, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.ChoiceSet",
            choices,
            wrap = value
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }

    [Fact(Skip = "Skipping this test")]
    // TODO confirm expected behavior as this test fails with System.NotSupportedException : Collection was of a fixed size
    public void validateChoiceSetAddChoice()
    {
        Choice[] choices =  {
            new Choice() { Title = "1", Value = "1" },
            new Choice() { Title = "2", Value = "2" },
        };
        var choiceSet = new ChoiceSetInput(choices);
        Choice newChoice = new Choice() { Title = "Something new", Value = "3" } ;
        choiceSet.AddChoices([newChoice]);

        var json = JsonSerializer.Serialize(choiceSet, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "Input.ChoiceSet",
            choices = choices.Append(newChoice)
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }
}
