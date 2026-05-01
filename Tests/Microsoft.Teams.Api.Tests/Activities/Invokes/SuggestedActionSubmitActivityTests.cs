#pragma warning disable ExperimentalTeamsSuggestedAction

using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

namespace Microsoft.Teams.Api.Tests.Activities;

public class SuggestedActionSubmitActivityTests
{
    [Fact]
    public void SuggestedActionSubmitActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SuggestedActionSubmitActivity.json");

        var activity = JsonSerializer.Deserialize<SuggestedActionSubmitActivity>(json);

        Assert.NotNull(activity);
        Assert.Equal("suggestedActionSubmitId", activity!.Id);
        Assert.Equal("channelId", activity.ChannelId.Value);
        Assert.Equal("suggestedAction/submit", activity.Name.Value);
        Assert.NotNull(activity.Value);
    }

    [Fact]
    public void SuggestedActionSubmitActivity_JsonDeserialize_DispatchedFromInvokeBase()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SuggestedActionSubmitActivity.json");

        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);

        Assert.IsType<SuggestedActionSubmitActivity>(activity);
    }

    [Fact]
    public void SuggestedActionSubmitActivity_JsonDeserialize_DispatchedFromActivityBase()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SuggestedActionSubmitActivity.json");

        var activity = JsonSerializer.Deserialize<Activity>(json);

        Assert.IsType<SuggestedActionSubmitActivity>(activity);
    }

    [Fact]
    public void SuggestedActionSubmitActivity_GetPath()
    {
        var activity = new SuggestedActionSubmitActivity();

        Assert.Equal("Activity.Invoke.SuggestedAction/submit", activity.GetPath());
    }
}
