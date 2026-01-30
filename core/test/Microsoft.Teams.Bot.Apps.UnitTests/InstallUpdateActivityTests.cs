// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.InstallActivities;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class InstallUpdateActivityTests
{
    [Fact]
    public void Constructor_Default_SetsInstallationUpdateType()
    {
        InstallUpdateActivity activity = new();
        Assert.Equal(TeamsActivityType.InstallationUpdate, activity.Type);
    }

    [Fact]
    public void DeserializeInstallUpdateFromJson_AddAction()
    {
        string json = """
        {
            "type": "installationUpdate",
            "conversation": {
                "id": "19"
            },
            "action": "add"
        }
        """;
        InstallUpdateActivity act = InstallUpdateActivity.FromJsonString(json);
        Assert.NotNull(act);
        Assert.Equal("installationUpdate", act.Type);
        Assert.Equal("add", act.Action);
    }

    [Fact]
    public void DeserializeInstallUpdateFromJson_RemoveAction()
    {
        string json = """
        {
            "type": "installationUpdate",
            "conversation": {
                "id": "19"
            },
            "action": "remove"
        }
        """;
        InstallUpdateActivity act = InstallUpdateActivity.FromJsonString(json);
        Assert.NotNull(act);
        Assert.Equal("installationUpdate", act.Type);
        Assert.Equal("remove", act.Action);
    }

    [Fact]
    public void SerializeInstallUpdateToJson()
    {
        var activity = new InstallUpdateActivity
        {
            Action = InstallUpdateActions.Add
        };

        string json = activity.ToJson();
        Assert.Contains("\"type\": \"installationUpdate\"", json);
        Assert.Contains("\"action\": \"add\"", json);
    }

    [Fact]
    public void FromActivityConvertsCorrectly()
    {
        var coreActivity = new CoreActivity
        {
            Type = TeamsActivityType.InstallationUpdate
        };
        coreActivity.Properties["action"] = "remove";

        InstallUpdateActivity activity = InstallUpdateActivity.FromActivity(coreActivity);
        Assert.NotNull(activity);
        Assert.Equal(TeamsActivityType.InstallationUpdate, activity.Type);
        Assert.Equal("remove", activity.Action);
    }

    [Fact]
    public void InstallUpdateActivity_SerializedAsCoreActivity_IncludesAction()
    {
        InstallUpdateActivity installUpdateActivity = new()
        {
            Action = InstallUpdateActions.Add,
            Type = TeamsActivityType.InstallationUpdate,
            ServiceUrl = new Uri("https://test.service.url/")
        };

        CoreActivity coreActivity = installUpdateActivity;
        string json = coreActivity.ToJson();

        Assert.Contains("\"action\"", json);
        Assert.Contains("add", json);
        Assert.Contains("\"type\": \"installationUpdate\"", json);
    }
}
