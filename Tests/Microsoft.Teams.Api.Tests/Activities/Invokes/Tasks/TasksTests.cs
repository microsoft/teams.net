using System.Text.Json;

using Microsoft.Teams.Api.Activities.Invokes;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes.Tasks;

public class TasksTests
{
    private static TaskActivity? Deserialize(string json) => JsonSerializer.Deserialize<TaskActivity>(json);

    [Fact]
    public void Task_MissingName_Throws()
    {
        var json = "{\"type\":\"invoke\"}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("must have a 'name'", ex.Message);
    }

    [Fact]
    public void Task_NullName_Throws()
    {
        var json = "{\"type\":\"invoke\",\"name\":null}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("failed to deserialize invoke activity 'name' property", ex.Message);
    }

    [Fact]
    public void Task_UnknownName_Throws()
    {
        var json = "{\"type\":\"invoke\",\"name\":\"task/other\"}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("doesn't match any known types", ex.Message);
    }
}