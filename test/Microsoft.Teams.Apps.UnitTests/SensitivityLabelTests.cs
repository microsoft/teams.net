// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class SensitivityLabelTests
{
    private const string LabelId = "11111111-2222-3333-4444-555555555555";

    private static JsonArray GetEntities(MessageActivityInput activity)
    {
        JsonNode? root = JsonNode.Parse(activity.ToJson());
        JsonArray? entities = root?["entities"]?.AsArray();
        Assert.NotNull(entities);
        return entities;
    }

    private static List<JsonNode> GetRootMessageEntities(JsonArray entities)
        => entities
            .Where(e => e?["type"]?.GetValue<string>() == "https://schema.org/Message" && e?["@type"]?.GetValue<string>() == "Message")
            .Select(e => e!)
            .ToList();

    [Fact]
    public void AddSensitivityLabel_SetsUsageInfoOnRootMessageEntity()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddSensitivityLabel(new SensitivityUsageInfo { Id = LabelId });

        JsonArray entities = GetEntities(activity);
        JsonNode entity = Assert.Single(entities)!;

        Assert.Equal("https://schema.org/Message", entity["type"]?.GetValue<string>());
        Assert.Equal("Message", entity["@type"]?.GetValue<string>());
        Assert.Equal("https://schema.org", entity["@context"]?.GetValue<string>());

        JsonObject usageInfo = entity["usageInfo"]!.AsObject();
        Assert.Equal(2, usageInfo.Count);
        Assert.Equal("CreativeWork", usageInfo["@type"]?.GetValue<string>());
        Assert.Equal(LabelId, usageInfo["@id"]?.GetValue<string>());
    }

    [Fact]
    public void AddSensitivityLabel_IncludesNameAndDescription()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddSensitivityLabel(new SensitivityUsageInfo { Id = LabelId, Name = "Confidential", Description = "Internal only" });

        JsonNode usageInfo = Assert.Single(GetEntities(activity))!["usageInfo"]!;

        Assert.Equal(LabelId, usageInfo["@id"]?.GetValue<string>());
        Assert.Equal("Confidential", usageInfo["name"]?.GetValue<string>());
        Assert.Equal("Internal only", usageInfo["description"]?.GetValue<string>());
    }

    [Fact]
    public void AddSensitivityLabel_PassesThroughCustomType()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddSensitivityLabel(new SensitivityUsageInfo { Type = "CustomType", Id = LabelId });

        JsonNode usageInfo = Assert.Single(GetEntities(activity))!["usageInfo"]!;

        Assert.Equal("CustomType", usageInfo["@type"]?.GetValue<string>());
        Assert.Equal(LabelId, usageInfo["@id"]?.GetValue<string>());
    }

    [Fact]
    public void AddSensitivityLabel_SecondCallReplacesFirst()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddSensitivityLabel(new SensitivityUsageInfo { Id = "first" })
            .AddSensitivityLabel(new SensitivityUsageInfo { Id = "second" });

        JsonNode entity = Assert.Single(GetEntities(activity))!;

        Assert.Equal("second", entity["usageInfo"]?["@id"]?.GetValue<string>());
    }

    public static TheoryData<string> Orderings => new() { "label-ai-citation", "label-citation-ai", "ai-label-citation", "ai-citation-label", "citation-label-ai", "citation-ai-label" };

    [Theory]
    [MemberData(nameof(Orderings))]
    public void AddSensitivityLabel_CombinedWithAIAndCitation_ProducesSingleRootEntity(string ordering)
    {
        MessageActivityInput activity = new();
        foreach (string step in ordering.Split('-'))
        {
            activity = step switch
            {
                "label" => activity.AddSensitivityLabel(new SensitivityUsageInfo { Id = LabelId }),
                "ai" => activity.AddAIGenerated(),
                "citation" => activity.AddCitation(1, new CitationAppearance { Name = "Doc", Abstract = "Abstract" }),
                _ => throw new ArgumentOutOfRangeException(nameof(ordering)),
            };
        }

        JsonArray entities = GetEntities(activity);
        JsonNode root = Assert.Single(GetRootMessageEntities(entities));

        Assert.Single(entities);
        Assert.Equal(LabelId, root["usageInfo"]?["@id"]?.GetValue<string>());
        Assert.Contains("AIGeneratedContent", root["additionalType"]!.AsArray().Select(n => n!.GetValue<string>()));
        Assert.Single(root["citation"]!.AsArray());
    }

    [Fact]
    public void AddCitation_UsageInfoWithIdOnly_SerializesWithoutRootEntityFields()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddCitation(1, new CitationAppearance
            {
                Name = "Doc",
                Abstract = "Abstract",
                SensitivityUsageInfo = new SensitivityUsageInfo { Id = LabelId },
            });

        JsonNode entity = Assert.Single(GetEntities(activity))!;
        JsonObject usageInfo = entity["citation"]![0]!["appearance"]!["usageInfo"]!.AsObject();

        Assert.Equal(2, usageInfo.Count);
        Assert.Equal("CreativeWork", usageInfo["@type"]?.GetValue<string>());
        Assert.Equal(LabelId, usageInfo["@id"]?.GetValue<string>());
    }

    [Fact]
    public void AddCitation_UsageInfoWithCustomType_PassesThrough()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddCitation(1, new CitationAppearance
            {
                Name = "Doc",
                Abstract = "Abstract",
                SensitivityUsageInfo = new SensitivityUsageInfo { Type = "CustomType", Id = LabelId },
            });

        JsonNode usageInfo = Assert.Single(GetEntities(activity))!["citation"]![0]!["appearance"]!["usageInfo"]!;

        Assert.Equal("CustomType", usageInfo["@type"]?.GetValue<string>());
        Assert.Equal(LabelId, usageInfo["@id"]?.GetValue<string>());
    }

#pragma warning disable CS0618 // exercising the obsolete CitationAppearance.UsageInfo
    [Fact]
    public void AddCitation_ObsoleteUsageInfo_StillSerializes()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddCitation(1, new CitationAppearance
            {
                Name = "Doc",
                Abstract = "Abstract",
                UsageInfo = new SensitiveUsageEntity { Name = "Confidential", Description = "Internal only" },
            });

        string json = activity.ToJson();

        Assert.Contains("\"usageInfo\"", json);
        Assert.Contains("\"Confidential\"", json);
    }

    [Fact]
    public void AddCitation_SensitivityUsageInfo_TakesPrecedenceOverObsoleteUsageInfo()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddCitation(1, new CitationAppearance
            {
                Name = "Doc",
                Abstract = "Abstract",
                UsageInfo = new SensitiveUsageEntity { Name = "Legacy" },
                SensitivityUsageInfo = new SensitivityUsageInfo { Id = LabelId },
            });

        JsonObject usageInfo = Assert.Single(GetEntities(activity))!["citation"]![0]!["appearance"]!["usageInfo"]!.AsObject();

        Assert.Equal(LabelId, usageInfo["@id"]?.GetValue<string>());
        Assert.False(usageInfo.ContainsKey("name"));
    }

    [Fact]
    public void CitationAppearanceDocument_CopyConstructor_CopiesObsoleteUsageInfo()
    {
        CitationAppearanceDocument original = new() { Name = "Doc", Abstract = "Abstract", UsageInfo = new SensitiveUsageEntity { Name = "Confidential" } };

        CitationAppearanceDocument copy = new(original);

        Assert.Equal("Confidential", copy.UsageInfo?.Name);
    }

    [Fact]
    public void Copy_ObsoleteUsageInfo_KeepsLegacyShape()
    {
        MessageActivityInput activity = new MessageActivityInput { Text = "Hello" }
            .AddCitation(1, new CitationAppearance
            {
                Name = "Doc",
                Abstract = "Abstract",
                UsageInfo = new SensitiveUsageEntity
                {
                    Name = "Confidential",
                    Pattern = new DefinedTerm { InDefinedTermSet = "set", Name = "term", TermCode = "code" },
                },
            });

        MessageActivityInput copy = activity.Copy();

        Assert.Equal(activity.ToJson(), copy.ToJson());
        CitationAppearanceDocument appearance = Assert.Single(copy.Entities!.OfType<CitationEntity>().Single().Citation!).Appearance;
        Assert.Equal("Confidential", appearance.UsageInfo?.Name);
        Assert.Equal("code", appearance.UsageInfo?.Pattern?.TermCode);
        Assert.Null(appearance.SensitivityUsageInfo);
    }
#pragma warning restore CS0618

    [Fact]
    public void AddSensitivityLabel_RoundTripsThroughDeserialization()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddAIGenerated()
            .AddSensitivityLabel(new SensitivityUsageInfo { Id = LabelId, Name = "Confidential" });

        TeamsActivity parsed = TeamsActivity.FromActivity(CoreActivity.FromJsonString(activity.ToJson()));

        OMessageEntity root = Assert.IsAssignableFrom<OMessageEntity>(Assert.Single(parsed.Entities!));
        Assert.NotNull(root.UsageInfo);
        Assert.Equal(LabelId, root.UsageInfo.Id);
        Assert.Equal("Confidential", root.UsageInfo.Name);
    }

    [Fact]
    public void AddSensitivityLabel_ObsoleteOverload_StillAddsSensitiveUsageEntity()
    {
#pragma warning disable CS0618
        MessageActivityInput activity = new MessageActivityInput()
            .AddSensitivityLabel("Confidential", "Internal only");
#pragma warning restore CS0618

        SensitiveUsageEntity entity = Assert.IsType<SensitiveUsageEntity>(Assert.Single(activity.Entities!));
        Assert.Equal("Confidential", entity.Name);
    }

    [Fact]
    public void AddSensitivityLabel_CopiesLabel()
    {
        SensitivityUsageInfo label = new() { Id = LabelId, Name = "Confidential" };
        MessageActivityInput activity = new MessageActivityInput().AddSensitivityLabel(label);

        label.Name = "Changed";

        JsonNode usageInfo = Assert.Single(GetEntities(activity))!["usageInfo"]!;
        Assert.Equal("Confidential", usageInfo["name"]?.GetValue<string>());
    }

    [Fact]
    public void AddCitation_CopiesUsageInfo()
    {
        SensitivityUsageInfo label = new() { Id = LabelId, Name = "Confidential" };
        MessageActivityInput activity = new MessageActivityInput()
            .AddCitation(1, new CitationAppearance { Name = "Doc", Abstract = "Abstract", SensitivityUsageInfo = label });

        label.Name = "Changed";

        JsonNode citation = GetEntities(activity).Single(e => e?["citation"] is not null)!;
        Assert.Equal("Confidential", citation["citation"]![0]!["appearance"]!["usageInfo"]!["name"]?.GetValue<string>());
    }

    [Fact]
    public void CitationAppearanceDocument_CopyConstructor_CopiesUsageInfo()
    {
        CitationAppearanceDocument original = new() { Name = "Doc", Abstract = "Abstract", SensitivityUsageInfo = new SensitivityUsageInfo { Id = LabelId, Name = "Confidential" } };
        CitationAppearanceDocument copy = new(original);

        original.SensitivityUsageInfo!.Name = "Changed";

        Assert.Equal("Confidential", copy.SensitivityUsageInfo?.Name);
    }

    [Fact]
    public void AddSensitivityLabel_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MessageActivityInput().AddSensitivityLabel((SensitivityUsageInfo)null!));
    }

    [Fact]
    public void AddSensitivityLabel_SerializesPosition()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddSensitivityLabel(new SensitivityUsageInfo { Id = LabelId, Position = 2 });

        JsonNode usageInfo = Assert.Single(GetEntities(activity))!["usageInfo"]!;

        Assert.Equal(2, usageInfo["position"]?.GetValue<int>());
    }

    private const string InboundUsageInfoWithoutId = "{\"@type\":\"CreativeWork\",\"name\":\"Confidential\"}";

    private static string InboundMessage(string extra)
        => "{\"type\":\"message\",\"id\":\"1\",\"channelId\":\"msteams\",\"conversation\":{\"id\":\"c\"},\"entities\":[{\"type\":\"https://schema.org/Message\",\"@type\":\"Message\",\"@context\":\"https://schema.org\"" + extra + "}]}";

    [Fact]
    public void Deserialize_MessageUsageInfoWithoutId_Parses()
    {
        TeamsActivity parsed = TeamsActivity.FromActivity(CoreActivity.FromJsonString(InboundMessage(",\"usageInfo\":" + InboundUsageInfoWithoutId)));

        OMessageEntity root = Assert.IsAssignableFrom<OMessageEntity>(Assert.Single(parsed.Entities!));
        Assert.NotNull(root.UsageInfo);
        Assert.Equal(string.Empty, root.UsageInfo.Id);
        Assert.Equal("Confidential", root.UsageInfo.Name);
    }

    [Fact]
    public void Deserialize_CitationUsageInfoWithoutId_Parses()
    {
        string citation = ",\"citation\":[{\"@type\":\"Claim\",\"position\":1,\"appearance\":{\"@type\":\"DigitalDocument\",\"name\":\"Doc\",\"abstract\":\"Abstract\",\"usageInfo\":" + InboundUsageInfoWithoutId + "}}]";

        TeamsActivity parsed = TeamsActivity.FromActivity(CoreActivity.FromJsonString(InboundMessage(citation)));

        CitationEntity entity = Assert.IsType<CitationEntity>(Assert.Single(parsed.Entities!));
        SensitivityUsageInfo? usageInfo = Assert.Single(entity.Citation!).Appearance.SensitivityUsageInfo;
        Assert.NotNull(usageInfo);
        Assert.Equal(string.Empty, usageInfo.Id);
        Assert.Equal("Confidential", usageInfo.Name);
    }
}
