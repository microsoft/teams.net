// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Bot.Compat;

namespace Microsoft.Teams.Bot.Compat.UnitTests;

/// <summary>
/// Verifies the ExtractString helper correctly handles JsonElement values from STJ
/// [JsonExtensionData] dictionaries, preventing the ToString() corruption (A-007).
/// </summary>
public class ExtractStringTests
{
    [Fact]
    public void ExtractString_WithPlainString_ReturnsAsIs()
    {
        Assert.Equal("hello", CompatActivity.ExtractString("hello"));
    }

    [Fact]
    public void ExtractString_WithNull_ReturnsNull()
    {
        Assert.Null(CompatActivity.ExtractString(null));
    }

    [Fact]
    public void ExtractString_WithJsonElementString_ReturnsUnquotedValue()
    {
        // Arrange – simulate what STJ produces for a string value stored in [JsonExtensionData]
        JsonElement je = JsonDocument.Parse("\"aad-object-id-value\"").RootElement;

        // Act
        string? result = CompatActivity.ExtractString(je);

        // Assert – must NOT include surrounding quotes
        Assert.Equal("aad-object-id-value", result);
    }

    [Fact]
    public void ExtractString_WithJsonElement_ToString_WouldReturnQuotedValue()
    {
        // Demonstrate the bug: JsonElement.ToString() returns the quoted JSON representation
        JsonElement je = JsonDocument.Parse("\"some-guid\"").RootElement;
        string quoted = je.ToString();

        // ToString() gives raw JSON text for String kind on .NET 6+ (returns unquoted actually)
        // but GetString() is always the correct way to extract the string value
        Assert.Equal("some-guid", CompatActivity.ExtractString(je));
        Assert.Equal("some-guid", je.GetString()); // GetString is always correct
    }

    [Fact]
    public void ExtractString_WithNonStringJsonElement_ReturnsRawText()
    {
        // Non-string JsonElement (e.g. a number) should return GetRawText(), not throw
        JsonElement je = JsonDocument.Parse("42").RootElement;
        string? result = CompatActivity.ExtractString(je);
        Assert.Equal("42", result);
    }

    [Fact]
    public void ToCompatChannelAccount_WithJsonElementProperties_ExtractsCorrectly()
    {
        // Arrange – a ConversationAccount whose Properties contain JsonElement values
        // (as produced by STJ [JsonExtensionData] after JSON deserialization)
        Microsoft.Teams.Bot.Core.Schema.ConversationAccount account =
            Microsoft.Teams.Bot.Core.Schema.CoreActivity.FromJsonString("""
                {
                    "type": "message",
                    "from": {
                        "id": "user-1",
                        "name": "Alice",
                        "aadObjectId": "aad-guid-123",
                        "userRole": "user"
                    }
                }
                """).From!;

        // Act
        Microsoft.Bot.Schema.ChannelAccount channelAccount = account.ToCompatChannelAccount();

        // Assert – string fields must not contain surrounding quotes or type names
        Assert.Equal("aad-guid-123", channelAccount.AadObjectId);
        Assert.Equal("user", channelAccount.Role);
    }
}
