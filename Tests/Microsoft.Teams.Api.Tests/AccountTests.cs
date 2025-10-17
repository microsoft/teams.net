// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Teams.Api.Tests;

public class AccountTests
{
    [Fact]
    public void Account_AgenticUserId_SetAndGet()
    {
        // Arrange
        var account = new Account() { Id = "testId" };
        var agenticUserId = "agentic-user-123";

        // Act
        account.AgenticUserId = agenticUserId;

        // Assert
        Assert.Equal(agenticUserId, account.AgenticUserId);
    }

    [Fact]
    public void Account_AgenticAppId_SetAndGet()
    {
        // Arrange
        var account = new Account() { Id = "testId" };
        var agenticAppId = "agentic-app-456";

        // Act
        account.AgenticAppId = agenticAppId;

        // Assert
        Assert.Equal(agenticAppId, account.AgenticAppId);
    }

    [Fact]
    public void Account_AgenticUserId_Nullable()
    {
        // Arrange & Act
        var account = new Account() { Id = "testId" };

        // Assert
        Assert.Null(account.AgenticUserId);
    }

    [Fact]
    public void Account_AgenticAppId_Nullable()
    {
        // Arrange & Act
        var account = new Account() { Id = "testId" };

        // Assert
        Assert.Null(account.AgenticAppId);
    }

    [Fact]
    public void Account_JsonSerialize_WithAgenticProperties()
    {
        // Arrange
        var account = new Account()
        {
            Id = "account-123",
            Name = "Test Account",
            Role = Role.AgenticUser,
            AgenticUserId = "agentic-user-789",
            AgenticAppId = "agentic-app-abc"
        };

        // Act
        var json = JsonSerializer.Serialize(account, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        // Assert
        Assert.Contains("\"agenticUserId\": \"agentic-user-789\"", json);
        Assert.Contains("\"agenticAppId\": \"agentic-app-abc\"", json);
    }

    [Fact]
    public void Account_JsonSerialize_WithoutAgenticProperties()
    {
        // Arrange
        var account = new Account()
        {
            Id = "account-123",
            Name = "Test Account",
            Role = Role.User
        };

        // Act
        var json = JsonSerializer.Serialize(account, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        // Assert
        Assert.DoesNotContain("agenticUserId", json);
        Assert.DoesNotContain("agenticAppId", json);
    }

    [Fact]
    public void Account_JsonDeserialize_WithAgenticProperties()
    {
        // Arrange
        var json = @"{
  ""id"": ""account-123"",
  ""role"": ""agenticUser"",
  ""name"": ""Test Account"",
  ""agenticUserId"": ""agentic-user-789"",
  ""agenticAppId"": ""agentic-app-abc""
}";

        // Act
        var account = JsonSerializer.Deserialize<Account>(json);

        // Assert
        Assert.NotNull(account);
        Assert.Equal("account-123", account.Id);
        Assert.Equal("agentic-user-789", account.AgenticUserId);
        Assert.Equal("agentic-app-abc", account.AgenticAppId);
    }

    [Fact]
    public void Account_JsonDeserialize_WithoutAgenticProperties()
    {
        // Arrange
        var json = @"{
  ""id"": ""account-123"",
  ""role"": ""user"",
  ""name"": ""Test Account""
}";

        // Act
        var account = JsonSerializer.Deserialize<Account>(json);

        // Assert
        Assert.NotNull(account);
        Assert.Equal("account-123", account.Id);
        Assert.Null(account.AgenticUserId);
        Assert.Null(account.AgenticAppId);
    }

    [Fact]
    public void Account_JsonPropertyOrder_AgenticProperties()
    {
        // Arrange
        var account = new Account()
        {
            Id = "account-123",
            AadObjectId = "aad-obj-id",
            Role = Role.AgenticUser,
            Name = "Test Account",
            AgenticUserId = "agentic-user-789",
            AgenticAppId = "agentic-app-abc",
            Properties = new Dictionary<string, object> { { "key", "value" } }
        };

        // Act
        var json = JsonSerializer.Serialize(account, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        // Assert - Verify property order (agenticUserId before agenticAppId before properties)
        var agenticUserIdIndex = json.IndexOf("\"agenticUserId\"");
        var agenticAppIdIndex = json.IndexOf("\"agenticAppId\"");
        var propertiesIndex = json.IndexOf("\"properties\"");

        Assert.True(agenticUserIdIndex < agenticAppIdIndex, "agenticUserId should appear before agenticAppId");
        Assert.True(agenticAppIdIndex < propertiesIndex, "agenticAppId should appear before properties");
    }

    [Fact]
    public void Account_WithAgenticRole_HasAgenticProperties()
    {
        // Arrange & Act
        var account = new Account()
        {
            Id = "account-123",
            Role = Role.AgenticInstance,
            AgenticUserId = "agentic-user-789",
            AgenticAppId = "agentic-app-abc"
        };

        // Assert
        Assert.NotNull(account.Role);
        Assert.True(account.Role.IsAgenticInstance);
        Assert.Equal("agentic-user-789", account.AgenticUserId);
        Assert.Equal("agentic-app-abc", account.AgenticAppId);
    }

    [Fact]
    public void Account_RoundTrip_SerializeDeserialize_WithAgenticProperties()
    {
        // Arrange
        var originalAccount = new Account()
        {
            Id = "account-123",
            AadObjectId = "aad-obj-id",
            Role = Role.AgenticUser,
            Name = "Test Account",
            AgenticUserId = "agentic-user-789",
            AgenticAppId = "agentic-app-abc"
        };

        // Act
        var json = JsonSerializer.Serialize(originalAccount);
        var deserializedAccount = JsonSerializer.Deserialize<Account>(json);

        // Assert
        Assert.NotNull(deserializedAccount);
        Assert.Equal(originalAccount.Id, deserializedAccount.Id);
        Assert.Equal(originalAccount.AadObjectId, deserializedAccount.AadObjectId);
        Assert.Equal(originalAccount.Name, deserializedAccount.Name);
        Assert.Equal(originalAccount.AgenticUserId, deserializedAccount.AgenticUserId);
        Assert.Equal(originalAccount.AgenticAppId, deserializedAccount.AgenticAppId);
    }
}
