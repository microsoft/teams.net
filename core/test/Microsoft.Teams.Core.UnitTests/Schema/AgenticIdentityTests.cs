// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.UnitTests.Schema;

public class AgenticIdentityTests
{
    [Fact]
    public void FromAgenticUser_CreatesUserScopedIdentity()
    {
        AgenticUser user = new()
        {
            AgenticAppInstanceId = "agent-app-instance",
            AgenticUserId = "agent-user",
            AgenticBlueprintId = "blueprint",
            TenantId = "tenant"
        };

        AgenticIdentity identity = AgenticIdentity.FromAgenticUser(user);

        Assert.Equal(AgenticIdentityKind.AgenticUser, identity.Kind);
        Assert.Equal("agent-app-instance", identity.AgenticAppInstanceId);
        Assert.Equal("agent-user", identity.AgenticUserId);
        Assert.Equal("blueprint", identity.AgenticBlueprintId);
        Assert.Equal("tenant", identity.TenantId);
    }

    [Fact]
    public void FromChannelRecipient_CreatesUserScopedIdentity()
    {
        ChannelAccount recipient = new()
        {
            AgenticAppInstanceId = "agent-app-instance",
            AgenticUserId = "agent-user",
            AgenticBlueprintId = "blueprint",
            TenantId = "tenant"
        };

        AgenticIdentity identity = AgenticIdentity.FromChannelRecipient(recipient);

        Assert.Equal(AgenticIdentityKind.AgenticUser, identity.Kind);
        Assert.Equal("agent-app-instance", identity.AgenticAppInstanceId);
        Assert.Equal("agent-user", identity.AgenticUserId);
        Assert.Equal("blueprint", identity.AgenticBlueprintId);
        Assert.Equal("tenant", identity.TenantId);
    }

    [Fact]
    public void FromAgenticUser_RequiresCompleteUserScope()
    {
        AgenticUser user = new()
        {
            AgenticAppInstanceId = "agent-app-instance"
        };

        Assert.ThrowsAny<ArgumentException>(() => AgenticIdentity.FromAgenticUser(user));
    }

    [Fact]
    public void TryGet_WithAgenticUserScope_ReturnsConcreteAgenticUser()
    {
        AgenticIdentity identity = AgenticIdentity.FromAgenticUser(new AgenticUser
        {
            AgenticAppInstanceId = "agent-app-instance",
            AgenticUserId = "agent-user",
            AgenticBlueprintId = "blueprint",
            TenantId = "tenant"
        });

        bool result = identity.TryGet(out AgenticUser? agenticUser);

        Assert.True(result);
        Assert.NotNull(agenticUser);
        Assert.Equal("agent-app-instance", agenticUser.AgenticAppInstanceId);
        Assert.Equal("agent-user", agenticUser.AgenticUserId);
        Assert.Equal("blueprint", agenticUser.AgenticBlueprintId);
        Assert.Equal("tenant", agenticUser.TenantId);
    }
}
