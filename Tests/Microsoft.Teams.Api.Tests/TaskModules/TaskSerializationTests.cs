using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Tests;

public class TaskSerializationTests
{
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };


    [Fact]
    public void Should_Serialize_Response_With_ContinueTask()
    {
        // arrange
        var taskInfo = new Microsoft.Teams.Api.TaskModules.TaskInfo
        {
            Title = "Test Dialog"
        };
        var continueTask = new Microsoft.Teams.Api.TaskModules.ContinueTask(taskInfo);
        var response = new Microsoft.Teams.Api.TaskModules.Response(continueTask);

        // act
        var json = JsonSerializer.Serialize(response, _jsonOptions);

        // assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("task", out var taskElement));
        Assert.Equal("continue", taskElement.GetProperty("type").GetString());
        Assert.True(taskElement.TryGetProperty("value", out var valueElement));
        Assert.Equal("Test Dialog", valueElement.GetProperty("title").GetString());
    }

    [Fact]
    public void Should_Serialize_Response_With_MessageTask()
    {
        // arrange
        var messageTask = new Microsoft.Teams.Api.TaskModules.MessageTask("Operation completed");
        var response = new Microsoft.Teams.Api.TaskModules.Response(messageTask);

        // act
        var json = JsonSerializer.Serialize(response, _jsonOptions);

        // assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("task", out var taskElement));
        Assert.Equal("message", taskElement.GetProperty("type").GetString());
        Assert.Equal("Operation completed", taskElement.GetProperty("value").GetString());
    }

    [Fact]
    public void Should_Deserialize_Response_With_ContinueTask()
    {
        // arrange
        string json = """
        {
            "task": {
                "type": "continue",
                "value": {
                    "title": "Test Dialog",
                    "url": "https://example.com"
                }
            },
            "cacheInfo": null
        }
        """;

        // act
        var response = JsonSerializer.Deserialize<Microsoft.Teams.Api.TaskModules.Response>(json, _jsonOptions);

        // assert
        Assert.NotNull(response);
        Assert.NotNull(response.Task);
        Assert.IsType<Microsoft.Teams.Api.TaskModules.ContinueTask>(response.Task);

        var continueTask = (Microsoft.Teams.Api.TaskModules.ContinueTask)response.Task;
        Assert.NotNull(continueTask.Value);
        Assert.Equal("Test Dialog", continueTask.Value.Title);
        Assert.Equal("https://example.com", continueTask.Value.Url);
    }

    [Fact]
    public void Should_Handle_TaskInfo_With_AdaptiveCard()
    {
        // arrange
        var card = new Microsoft.Teams.Cards.AdaptiveCard
        {
            Body = new List<Microsoft.Teams.Cards.CardElement>
            {
                new Microsoft.Teams.Cards.TextBlock("Test card")
            }
        };

        var taskInfo = new Microsoft.Teams.Api.TaskModules.TaskInfo
        {
            Title = "Card Dialog",
            Card = new Microsoft.Teams.Api.Attachment
            {
                ContentType = new Microsoft.Teams.Api.ContentType("application/vnd.microsoft.card.adaptive"),
                Content = card
            }
        };

        var continueTask = new Microsoft.Teams.Api.TaskModules.ContinueTask(taskInfo);

        // act
        var json = JsonSerializer.Serialize(continueTask, _jsonOptions);

        // assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("continue", root.GetProperty("type").GetString());
        Assert.True(root.TryGetProperty("value", out var valueElement));
        Assert.Equal("Card Dialog", valueElement.GetProperty("title").GetString());
        Assert.True(valueElement.TryGetProperty("card", out var cardElement));
        Assert.Equal("application/vnd.microsoft.card.adaptive", cardElement.GetProperty("contentType").GetString());
    }

    [Fact]
    public void Should_Throw_JsonException_For_Unknown_Task_Type()
    {
        // arrange
        string json = """
        {
            "type": "unknown_type",
            "value": "some value"
        }
        """;

        // act & assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Microsoft.Teams.Api.TaskModules.Task>(json, _jsonOptions));
    }

    [Fact]
    public void Should_Throw_JsonException_When_Type_Property_Missing()
    {
        // arrange
        string json = """
        {
            "value": "some value"
        }
        """;

        // act & assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Microsoft.Teams.Api.TaskModules.Task>(json, _jsonOptions));
    }

    [Fact]
    public void TaskFetchAction_Should_Merge_Properties()
    {
        // arrange - TaskFetchAction should merge properties into root data (like TypeScript version)
        var action = new Microsoft.Teams.Cards.TaskFetchAction(Microsoft.Teams.Cards.TaskFetchAction.FromObject(new { opendialogtype = "simple_form", customProperty = "value" }));

        // act
        var json = JsonSerializer.Serialize(action, _jsonOptions);

        // Debug: Print actual JSON to see structure
        System.Console.WriteLine($"Actual JSON: {json}");

        // assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("data", out var dataElement));
        Assert.True(dataElement.TryGetProperty("msTeams", out var msTeamsElement));
        Assert.Equal("task/fetch", msTeamsElement.GetProperty("type").GetString());

        // TaskFetchAction is special - it merges custom properties into the root SubmitActionData
        // This matches the TypeScript implementation behavior
        Assert.True(dataElement.TryGetProperty("opendialogtype", out var dialogTypeElement));
        Assert.Equal("simple_form", dialogTypeElement.GetString());
        Assert.True(dataElement.TryGetProperty("customProperty", out var customElement));
        Assert.Equal("value", customElement.GetString());
    }
}