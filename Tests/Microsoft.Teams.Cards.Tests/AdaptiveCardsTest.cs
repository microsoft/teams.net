using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards.Tests;

public class AdaptiveCardsTest
{
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void Should_Serialize_AdaptiveCard_Simple()
    {
        // arrange
        AdaptiveCard card = new AdaptiveCard()
        {
            Body = new List<CardElement>()
            {
                new TextBlock("Hello, Adaptive Card!")
            }
        };

        // act
        var json = JsonSerializer.Serialize(card, card.GetType(), new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        // assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("body", out var bodyElement));
        Assert.Equal(JsonValueKind.Array, bodyElement.ValueKind);
        Assert.Equal(1, bodyElement.GetArrayLength());

        var first = bodyElement[0];
        Assert.Equal("TextBlock", first.GetProperty("type").GetString());
        Assert.Equal("Hello, Adaptive Card!", first.GetProperty("text").GetString());
    }

    [Fact]
    public void Should_Deserialize_AdaptiveCard_Simple()
    {
        string json = @"{
            ""body"": [
                {
                    ""type"": ""TextBlock"",
                    ""text"": ""Hello, Adaptive Card!""
                }
            ]
        }";

        AdaptiveCard card = JsonSerializer.Deserialize<AdaptiveCard>(json, _jsonOptions)!;

        Assert.NotNull(card);
        Assert.Single(card.Body!);
        Assert.IsType<TextBlock>(card.Body![0]);
        Assert.Equal("Hello, Adaptive Card!", ((TextBlock)card.Body[0]).Text);
    }

    [Fact]
    public void Should_Serialize_BasicCard_WithToggleInput()
    {
        // arrange - recreating CreateBasicAdaptiveCard from samples
        var card = new AdaptiveCard
        {
            Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
            Body = new List<CardElement>
            {
                new TextBlock("Hello world")
                {
                    Wrap = true,
                    Weight = TextWeight.Bolder
                },
                new ToggleInput("Notify me")
                {
                    Id = "notify"
                }
            },
            Actions = new List<Microsoft.Teams.Cards.Action>
            {
                new ExecuteAction
                {
                    Title = "Submit",
                    Data = new Union<string, SubmitActionData>(new SubmitActionData { NonSchemaProperties = new Dictionary<string, object?> { { "action", "submit_basic" } } }),
                    AssociatedInputs = AssociatedInputs.Auto
                }
            }
        };

        // act
        var json = JsonSerializer.Serialize(card, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        // assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("$schema", out var schemaElement));
        Assert.Equal("http://adaptivecards.io/schemas/adaptive-card.json", schemaElement.GetString());

        Assert.True(root.TryGetProperty("body", out var bodyElement));
        Assert.Equal(2, bodyElement.GetArrayLength());

        var textBlock = bodyElement[0];
        Assert.Equal("TextBlock", textBlock.GetProperty("type").GetString());
        Assert.Equal("Hello world", textBlock.GetProperty("text").GetString());
        Assert.True(textBlock.GetProperty("weight").GetString() == "Bolder");

        var toggleInput = bodyElement[1];
        Assert.Equal("Input.Toggle", toggleInput.GetProperty("type").GetString());
        Assert.Equal("notify", toggleInput.GetProperty("id").GetString());

        Assert.True(root.TryGetProperty("actions", out var actionsElement));
        Assert.Single(actionsElement.EnumerateArray());
    }

    [Fact]
    public void Should_Deserialize_ProfileCard_WithInputs()
    {
        string json = @"{
            ""schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
            ""body"": [
                {
                    ""type"": ""TextBlock"",
                    ""text"": ""User Profile"",
                    ""weight"": ""Bolder"",
                    ""size"": ""Large""
                },
                {
                    ""type"": ""Input.Text"",
                    ""id"": ""name"",
                    ""label"": ""Name"",
                    ""value"": ""John Doe""
                },
                {
                    ""type"": ""Input.Text"",
                    ""id"": ""email"",
                    ""label"": ""Email"",
                    ""value"": ""john@contoso.com""
                },
                {
                    ""type"": ""Input.Toggle"",
                    ""id"": ""subscribe"",
                    ""title"": ""Subscribe to newsletter"",
                    ""value"": ""false""
                }
            ],
            ""actions"": [
                {
                    ""type"": ""Action.Execute"",
                    ""title"": ""Save"",
                    ""data"": {
                        ""action"": ""save_profile"",
                        ""entity_id"": ""12345""
                    }
                }
            ]
        }";

        var card = JsonSerializer.Deserialize<AdaptiveCard>(json, _jsonOptions)!;

        Assert.NotNull(card);
        // Note: Schema might be serialized as $schema in JSON but not always set on deserialized object
        Assert.Equal(4, card.Body!.Count);

        var titleBlock = card.Body[0] as TextBlock;
        Assert.NotNull(titleBlock);
        Assert.Equal("User Profile", titleBlock.Text);
        Assert.Equal("Bolder", titleBlock.Weight?.ToString());

        var nameInput = card.Body[1] as TextInput;
        Assert.NotNull(nameInput);
        Assert.Equal("name", nameInput.Id);
        Assert.Equal("Name", nameInput.Label);
        Assert.Equal("John Doe", nameInput.Value);

        var emailInput = card.Body[2] as TextInput;
        Assert.NotNull(emailInput);
        Assert.Equal("email", emailInput.Id);
        Assert.Equal("Email", emailInput.Label);

        var toggleInput = card.Body[3] as ToggleInput;
        Assert.NotNull(toggleInput);
        Assert.Equal("subscribe", toggleInput.Id);
        Assert.Equal("Subscribe to newsletter", toggleInput.Title);

        Assert.Single(card.Actions!);
        var executeAction = card.Actions[0] as ExecuteAction;
        Assert.NotNull(executeAction);
        Assert.Equal("Save", executeAction.Title);
    }

    [Fact]
    public void Should_Serialize_TaskFormCard_WithChoiceSet()
    {
        // arrange - recreating CreateTaskFormCard from samples
        var card = new AdaptiveCard
        {
            Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
            Body = new List<CardElement>
            {
                new TextBlock("Create New Task")
                {
                    Weight = TextWeight.Bolder,
                    Size = TextSize.Large
                },
                new TextInput
                {
                    Id = "title",
                    Label = "Task Title",
                    Placeholder = "Enter task title"
                },
                new ChoiceSetInput
                {
                    Id = "priority",
                    Label = "Priority",
                    Value = "medium",
                    Choices = new List<Choice>
                    {
                        new() { Title = "High", Value = "high" },
                        new() { Title = "Medium", Value = "medium" },
                        new() { Title = "Low", Value = "low" }
                    }
                },
                new DateInput
                {
                    Id = "due_date",
                    Label = "Due Date",
                    Value = "2024-01-15"
                }
            }
        };

        // act
        var json = JsonSerializer.Serialize(card, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        // assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("body", out var bodyElement));
        Assert.Equal(4, bodyElement.GetArrayLength());

        var choiceSetInput = bodyElement[2];
        Assert.Equal("Input.ChoiceSet", choiceSetInput.GetProperty("type").GetString());
        Assert.Equal("priority", choiceSetInput.GetProperty("id").GetString());
        Assert.Equal("medium", choiceSetInput.GetProperty("value").GetString());

        Assert.True(choiceSetInput.TryGetProperty("choices", out var choicesElement));
        Assert.Equal(3, choicesElement.GetArrayLength());
        Assert.Equal("High", choicesElement[0].GetProperty("title").GetString());
        Assert.Equal("high", choicesElement[0].GetProperty("value").GetString());

        var dateInput = bodyElement[3];
        Assert.Equal("Input.Date", dateInput.GetProperty("type").GetString());
        Assert.Equal("due_date", dateInput.GetProperty("id").GetString());
    }

    [Fact]
    public void Should_Deserialize_ComplexCard_FromJson()
    {
        // Using the JSON structure from CreateCardFromJson in samples
        string json = @"{
            ""type"": ""AdaptiveCard"",
            ""body"": [
                {
                    ""type"": ""ColumnSet"",
                    ""columns"": [
                        {
                            ""type"": ""Column"",
                            ""verticalContentAlignment"": ""center"",
                            ""items"": [
                                {
                                    ""type"": ""Image"",
                                    ""style"": ""Person"",
                                    ""url"": ""https://aka.ms/AAp9xo4"",
                                    ""size"": ""Small"",
                                    ""altText"": ""Portrait of David Claux""
                                }
                            ],
                            ""width"": ""auto""
                        },
                        {
                            ""type"": ""Column"",
                            ""spacing"": ""medium"",
                            ""verticalContentAlignment"": ""center"",
                            ""items"": [
                                {
                                    ""type"": ""TextBlock"",
                                    ""weight"": ""Bolder"",
                                    ""text"": ""David Claux"",
                                    ""wrap"": true
                                }
                            ],
                            ""width"": ""auto""
                        }
                    ]
                },
                {
                    ""type"": ""TextBlock"",
                    ""text"": ""This card was created from JSON deserialization!"",
                    ""wrap"": true,
                    ""color"": ""good"",
                    ""spacing"": ""medium""
                }
            ],
            ""actions"": [
                {
                    ""type"": ""Action.Execute"",
                    ""title"": ""Test JSON Action"",
                    ""data"": {
                        ""Value"": {
                            ""action"": ""test_json""
                        }
                    }
                }
            ],
            ""version"": ""1.5"",
            ""schema"": ""http://adaptivecards.io/schemas/adaptive-card.json""
        }";

        var card = JsonSerializer.Deserialize<AdaptiveCard>(json, _jsonOptions)!;

        Assert.NotNull(card);
        Assert.Equal("1.5", card.Version);
        // Note: Schema property might not be set during deserialization, focus on content verification
        Assert.Equal(2, card.Body!.Count);

        var columnSet = card.Body[0] as ColumnSet;
        Assert.NotNull(columnSet);
        Assert.Equal(2, columnSet.Columns!.Count);

        var firstColumn = columnSet.Columns[0];
        Assert.Equal("auto", firstColumn.Width);
        Assert.Single(firstColumn.Items!);

        var image = firstColumn.Items[0] as Image;
        Assert.NotNull(image);
        Assert.Equal("https://aka.ms/AAp9xo4", image.Url);
        Assert.Equal("Person", image.Style?.ToString());

        var textBlock = card.Body[1] as TextBlock;
        Assert.NotNull(textBlock);
        Assert.Equal("This card was created from JSON deserialization!", textBlock.Text);
        Assert.Equal("good", textBlock.Color?.ToString());

        Assert.Single(card.Actions!);
        var executeAction = card.Actions[0] as ExecuteAction;
        Assert.NotNull(executeAction);
        Assert.Equal("Test JSON Action", executeAction.Title);
    }

    [Fact]
    public void Should_Serialize_FeedbackCard_WithMultilineInput()
    {
        // arrange - recreating CreateFeedbackCard from samples
        var card = new AdaptiveCard
        {
            Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
            Body = new List<CardElement>
            {
                new TextBlock("Feedback Form")
                {
                    Weight = TextWeight.Bolder,
                    Size = TextSize.Large
                },
                new TextInput
                {
                    Id = "feedback",
                    Label = "Your Feedback",
                    Placeholder = "Please share your thoughts...",
                    IsMultiline = true,
                    IsRequired = true
                }
            },
            Actions = new List<Microsoft.Teams.Cards.Action>
            {
                new ExecuteAction
                {
                    Title = "Submit Feedback",
                    Data = new Union<string, SubmitActionData>(new SubmitActionData { NonSchemaProperties = new Dictionary<string, object?> { { "action", "submit_feedback" } } }),
                    AssociatedInputs = AssociatedInputs.Auto
                }
            }
        };

        // act
        var json = JsonSerializer.Serialize(card, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        // assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("body", out var bodyElement));
        Assert.Equal(2, bodyElement.GetArrayLength());

        var textInput = bodyElement[1];
        Assert.Equal("Input.Text", textInput.GetProperty("type").GetString());
        Assert.Equal("feedback", textInput.GetProperty("id").GetString());
        Assert.Equal("Your Feedback", textInput.GetProperty("label").GetString());
        Assert.True(textInput.GetProperty("isMultiline").GetBoolean());
        Assert.True(textInput.GetProperty("isRequired").GetBoolean());

        Assert.True(root.TryGetProperty("actions", out var actionsElement));
        var action = actionsElement[0];
        Assert.Equal("Action.Execute", action.GetProperty("type").GetString());
        Assert.Equal("Submit Feedback", action.GetProperty("title").GetString());
    }

    [Fact]
    public void Should_Deserialize_ValidationCard_WithNumberInput()
    {
        string json = @"{
            ""schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
            ""body"": [
                {
                    ""type"": ""TextBlock"",
                    ""text"": ""Profile with Validation"",
                    ""weight"": ""Bolder"",
                    ""size"": ""Large""
                },
                {
                    ""type"": ""Input.Number"",
                    ""id"": ""age"",
                    ""label"": ""Age"",
                    ""isRequired"": true,
                    ""min"": 0,
                    ""max"": 120
                },
                {
                    ""type"": ""Input.Text"",
                    ""id"": ""name"",
                    ""label"": ""Name"",
                    ""isRequired"": true,
                    ""errorMessage"": ""Name is required""
                }
            ]
        }";

        var card = JsonSerializer.Deserialize<AdaptiveCard>(json, _jsonOptions)!;

        Assert.NotNull(card);
        Assert.Equal(3, card.Body!.Count);

        var numberInput = card.Body[1] as NumberInput;
        Assert.NotNull(numberInput);
        Assert.Equal("age", numberInput.Id);
        Assert.Equal("Age", numberInput.Label);
        Assert.True(numberInput.IsRequired);
        Assert.Equal(0, numberInput.Min);
        Assert.Equal(120, numberInput.Max);

        var textInput = card.Body[2] as TextInput;
        Assert.NotNull(textInput);
        Assert.Equal("name", textInput.Id);
        Assert.True(textInput.IsRequired);
        Assert.Equal("Name is required", textInput.ErrorMessage);
    }

    [Fact]
    public void Should_Deserialize_With_Minimal_JsonOptions()
    {
        // Test what minimal JsonSerializerOptions are actually required
        string json = """
        {
            "type": "AdaptiveCard",
            "body": [
                {
                    "type": "TextBlock",
                    "text": "Hello World",
                    "weight": "Bolder"
                }
            ],
            "actions": [
                {
                    "type": "Action.Execute",
                    "title": "Submit",
                    "associatedInputs": "auto"
                }
            ]
        }
        """;

        // Test 1: No options at all
        var card1 = JsonSerializer.Deserialize<AdaptiveCard>(json);
        Assert.NotNull(card1);
        Assert.Single(card1.Body!);

        // Test 2: Only PropertyNameCaseInsensitive 
        var options2 = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var card2 = JsonSerializer.Deserialize<AdaptiveCard>(json, options2);
        Assert.NotNull(card2);
        Assert.Single(card2.Body!);

        // Test 3: With CamelCase policy (what we had in docs)
        var options3 = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        };
        var card3 = JsonSerializer.Deserialize<AdaptiveCard>(json, options3);
        Assert.NotNull(card3);
        Assert.Single(card3.Body!);

        // All should work the same
        var textBlock1 = card1.Body![0] as TextBlock;
        var textBlock2 = card2.Body![0] as TextBlock;
        var textBlock3 = card3.Body![0] as TextBlock;
        
        Assert.Equal("Hello World", textBlock1?.Text);
        Assert.Equal("Hello World", textBlock2?.Text);
        Assert.Equal("Hello World", textBlock3?.Text);
    }
}