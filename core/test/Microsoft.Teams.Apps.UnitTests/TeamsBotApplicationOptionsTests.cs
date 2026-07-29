// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.UnitTests;

public class TeamsBotApplicationOptionsTests
{
    [Fact]
    public void AddOAuthFlow_EnablesState()
    {
        TeamsBotApplicationOptions options = new();

        options.AddOAuthFlow("graph");

        Assert.True(options.IsStateEnabled);
    }

    [Fact]
    public void AddOAuthFlow_DoesNotOverwriteExistingStateConfiguration()
    {
        TeamsBotApplicationOptions options = new();
        options.UseState(s => s.KeyPrefix = "custom");

        options.AddOAuthFlow("graph");

        Assert.True(options.IsStateEnabled);
        Assert.NotNull(options.StateConfiguration);
    }

    [Fact]
    public void AddOAuthFlow_BeforeUseState_PreservesLaterConfiguration()
    {
        TeamsBotApplicationOptions options = new();

        options.AddOAuthFlow("graph");
        options.UseState(s => s.KeyPrefix = "custom");

        Assert.True(options.IsStateEnabled);
        Assert.NotNull(options.StateConfiguration);
    }

    [Fact]
    public void UseState_WithoutConfigure_SetsNullConfiguration()
    {
        TeamsBotApplicationOptions options = new();

        options.UseState();

        Assert.True(options.IsStateEnabled);
        Assert.Null(options.StateConfiguration);
    }
}
