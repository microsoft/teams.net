using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

using static Microsoft.Teams.Api.Activities.Invokes.Tasks;

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

    [Fact]
    public void Task_Fetch_Value_AccessibleFromDerivedType()
    {
        var json = "{\"type\":\"invoke\",\"name\":\"task/fetch\",\"value\":{\"context\":{\"theme\":\"default\"}}}";
        var activity = Deserialize(json);
        var fetch = Assert.IsType<FetchActivity>(activity);
        Assert.NotNull(fetch.Value);
    }

    [Fact]
    public void Task_Fetch_Value_AccessibleFromBaseType()
    {
        var json = "{\"type\":\"invoke\",\"name\":\"task/fetch\",\"value\":{\"context\":{\"theme\":\"default\"}}}";
        var activity = Deserialize(json);
        var invoke = Assert.IsAssignableFrom<InvokeActivity>(activity);
        Assert.NotNull(invoke.Value);
    }
}