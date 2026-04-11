// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

/// <summary>
/// Verifies that the CoreActivity copy constructor produces independent (deep) copies so that
/// mutations on the copy do not affect the original, and vice versa (COPY-01 / A-013).
/// </summary>
public class CoreActivityDeepCopyTests
{
    private static CoreActivity MakeActivity() => CoreActivity.FromJsonString("""
        {
            "type": "message",
            "channelId": "msteams",
            "id": "act-001",
            "from":        { "id": "user-1", "name": "Alice" },
            "recipient":   { "id": "bot-1",  "name": "Bot" },
            "conversation":{ "id": "conv-1"  },
            "channelData": { "tenant": { "id": "tenant-1" } },
            "entities":    [{ "type": "mention", "text": "@Bot" }],
            "attachments": [{ "contentType": "text/plain" }],
            "value":       { "key": "original" }
        }
        """);

    [Fact]
    public void CopyConstructor_From_IsIndependent()
    {
        CoreActivity original = MakeActivity();
        TeamsActivity copy = TeamsActivity.FromActivity(original);

        // Mutate the copy's From account
        copy.From!.Properties["aadObjectId"] = "new-aad-id";

        // Original must be unaffected
        Assert.False(original.From!.Properties.ContainsKey("aadObjectId"));
    }

    [Fact]
    public void CopyConstructor_Conversation_IsIndependent()
    {
        CoreActivity original = MakeActivity();
        TeamsActivity copy = TeamsActivity.FromActivity(original);

        copy.Conversation!.Properties["tenantId"] = "mutated";

        Assert.False(original.Conversation!.Properties.ContainsKey("tenantId"));
    }

    [Fact]
    public void CopyConstructor_Properties_IsIndependent()
    {
        CoreActivity original = MakeActivity();
        original.Properties["customKey"] = "original-value";

        TeamsActivity copy = TeamsActivity.FromActivity(original);

        copy.Properties["customKey"] = "mutated-value";

        Assert.Equal("original-value", original.Properties["customKey"]?.ToString());
    }

    [Fact]
    public void CopyConstructor_Value_IsIndependent()
    {
        CoreActivity original = MakeActivity();
        TeamsActivity copy = TeamsActivity.FromActivity(original);

        // Mutate the copy's Value node
        if (copy.Value is JsonObject obj)
            obj["key"] = "mutated";

        // Original value node must be unaffected
        if (original.Value is JsonObject origObj)
            Assert.Equal("original", origObj["key"]?.GetValue<string>());
    }

    // ── CitationEntity deep-copy (COPY-01 / A-015) ────────────────────────────

    [Fact]
    public void CitationEntity_CopyCtor_ClaimListIsIndependent()
    {
        Microsoft.Teams.Bot.Apps.Schema.Entities.CitationEntity original = new()
        {
            Citation =
            [
                new()
                {
                    Position = 1,
                    Appearance = new() { Name = "Doc1", Abstract = "Abs1" }
                }
            ]
        };

        Microsoft.Teams.Bot.Apps.Schema.Entities.CitationEntity copy = new(original);

        // Mutate the copy's claim — original must be unaffected
        copy.Citation![0].Position = 99;

        Assert.Equal(1, original.Citation![0].Position);
    }
}
