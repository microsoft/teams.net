// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Entities;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class CitationEntityTests
{
    [Fact]
    public void AddCitation_CreatesEntityWithClaim()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));

        var citation = activity.AddCitation(1, new CitationAppearance
        {
            Name = "Test Document",
            Abstract = "Test abstract content"
        });

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<CitationEntity>(activity.Entities[0]);
        Assert.NotNull(citation.Citation);
        Assert.Single(citation.Citation);
        Assert.Equal(1, citation.Citation[0].Position);
        Assert.Equal("Test Document", citation.Citation[0].Appearance.Name);
        Assert.Equal("Test abstract content", citation.Citation[0].Appearance.Abstract);
    }

    [Fact]
    public void AddCitation_MultipleCitations_AccumulateOnSameEntity()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));

        activity.AddCitation(1, new CitationAppearance
        {
            Name = "Document One",
            Abstract = "First abstract"
        });

        var citation = activity.AddCitation(2, new CitationAppearance
        {
            Name = "Document Two",
            Abstract = "Second abstract"
        });

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.NotNull(citation.Citation);
        Assert.Equal(2, citation.Citation.Count);
        Assert.Equal(1, citation.Citation[0].Position);
        Assert.Equal(2, citation.Citation[1].Position);
        Assert.Equal("Document One", citation.Citation[0].Appearance.Name);
        Assert.Equal("Document Two", citation.Citation[1].Appearance.Name);
    }

    [Fact]
    public void AddAIGenerated_SetsAdditionalType()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));

        var messageEntity = activity.AddAIGenerated();

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<OMessageEntity>(activity.Entities[0]);
        Assert.NotNull(messageEntity.AdditionalType);
        Assert.Contains("AIGeneratedContent", messageEntity.AdditionalType);
    }

    [Fact]
    public void AddAIGenerated_CalledTwice_DoesNotDuplicate()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));

        activity.AddAIGenerated();
        activity.AddAIGenerated();

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        var messageEntity = activity.Entities[0] as OMessageEntity;
        Assert.NotNull(messageEntity?.AdditionalType);
        Assert.Single(messageEntity.AdditionalType);
    }

    [Fact]
    public void AddAIGenerated_ThenAddCitation_PreservesAILabel()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));

        activity.AddAIGenerated();
        var citation = activity.AddCitation(1, new CitationAppearance
        {
            Name = "Test Doc",
            Abstract = "Test abstract"
        });

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<CitationEntity>(activity.Entities[0]);
        Assert.NotNull(citation.AdditionalType);
        Assert.Contains("AIGeneratedContent", citation.AdditionalType);
        Assert.NotNull(citation.Citation);
        Assert.Single(citation.Citation);
    }

    [Fact]
    public void AddFeedback_SetsFeedbackLoopEnabled()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));

        activity.AddFeedback();

        Assert.NotNull(activity.ChannelData);
        Assert.True(activity.ChannelData.FeedbackLoopEnabled);
    }

    [Fact]
    public void AddCitation_WithAllAppearanceFields_SetsCorrectly()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));

        var citation = activity.AddCitation(1, new CitationAppearance
        {
            Name = "Full Document",
            Abstract = "Full abstract",
            Text = "{\"type\":\"AdaptiveCard\"}",
            Url = new Uri("https://example.com/doc"),
            EncodingFormat = EncodingFormats.AdaptiveCard,
            Icon = CitationIcon.MicrosoftWord,
            Keywords = ["keyword1", "keyword2"],
            UsageInfo = new SensitiveUsageEntity { Name = "Confidential" }
        });

        Assert.NotNull(citation.Citation);
        var appearance = citation.Citation[0].Appearance;
        Assert.Equal("Full Document", appearance.Name);
        Assert.Equal("Full abstract", appearance.Abstract);
        Assert.Equal("{\"type\":\"AdaptiveCard\"}", appearance.Text);
        Assert.Equal(new Uri("https://example.com/doc"), appearance.Url);
        Assert.Equal(EncodingFormats.AdaptiveCard, appearance.EncodingFormat);
        Assert.NotNull(appearance.Image);
        Assert.Equal(CitationIcon.MicrosoftWord, appearance.Image.Name);
        Assert.NotNull(appearance.Keywords);
        Assert.Equal(2, appearance.Keywords.Count);
        Assert.NotNull(appearance.UsageInfo);
        Assert.Equal("Confidential", appearance.UsageInfo.Name);
    }

    [Fact]
    public void CitationEntity_RoundTrip_Serialization()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));

        activity.AddAIGenerated();
        activity.AddCitation(1, new CitationAppearance
        {
            Name = "Test Document",
            Abstract = "Test abstract content",
            Url = new Uri("https://example.com"),
            Icon = CitationIcon.Pdf,
            Keywords = ["test", "citation"]
        });
        activity.AddFeedback();

        string json = activity.ToJson();

        Assert.Contains("\"citation\"", json);
        Assert.Contains("Test Document", json);
        Assert.Contains("Test abstract content", json);
        Assert.Contains("https://example.com", json);
        Assert.Contains("AIGeneratedContent", json);
        Assert.Contains("Claim", json);
        Assert.Contains("DigitalDocument", json);
        Assert.Contains("PDF", json);
        Assert.Contains("feedbackLoopEnabled", json);
    }

    [Fact]
    public void CitationEntity_Rebase_SurvivesRoundTrip()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));

        activity.AddAIGenerated();
        activity.AddCitation(1, new CitationAppearance
        {
            Name = "Rebase Test Doc",
            Abstract = "Rebase test abstract",
            Icon = CitationIcon.MicrosoftExcel
        });

        // Verify base CoreActivity.Entities (JsonArray) contains the citation data
        CoreActivity coreActivity = activity;
        Assert.NotNull(coreActivity.Entities);
        Assert.Single(coreActivity.Entities);

        string? entityJson = coreActivity.Entities[0]?.ToJsonString();
        Assert.NotNull(entityJson);
        Assert.Contains("citation", entityJson);
        Assert.Contains("Rebase Test Doc", entityJson);
        Assert.Contains("Rebase test abstract", entityJson);
        Assert.Contains("AIGeneratedContent", entityJson);
        Assert.Contains("Microsoft Excel", entityJson);
    }

    [Fact]
    public void Fixture_AdaptiveCardActivity_DeserializesAIGeneratedEntity()
    {
        string json = """
        {
          "type": "message",
          "channelId": "msteams",
          "entities": [
            {
              "type": "https://schema.org/Message",
              "@context": "https://schema.org",
              "@type": "Message",
              "additionalType": [
                "AIGeneratedContent"
              ]
            }
          ]
        }
        """;

        CoreActivity coreActivity = CoreActivity.FromJsonString(json);
        TeamsActivity activity = TeamsActivity.FromActivity(coreActivity);

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);

        var entity = activity.Entities[0];
        Assert.Equal("https://schema.org/Message", entity.Type);
        Assert.Equal("Message", entity.OType);

        // Should deserialize as CitationEntity (since @type is "Message")
        var citationEntity = entity as CitationEntity;
        Assert.NotNull(citationEntity);
        Assert.NotNull(citationEntity.AdditionalType);
        Assert.Contains("AIGeneratedContent", citationEntity.AdditionalType);
    }

    [Fact]
    public void Fixture_SensitiveUsageEntity_DeserializesByOType()
    {
        string json = """
        {
          "type": "message",
          "entities": [
            {
              "type": "https://schema.org/Message",
              "@context": "https://schema.org",
              "@type": "CreativeWork",
              "name": "Confidential",
              "description": "This is sensitive content"
            }
          ]
        }
        """;

        CoreActivity coreActivity = CoreActivity.FromJsonString(json);
        TeamsActivity activity = TeamsActivity.FromActivity(coreActivity);

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);

        var entity = activity.Entities[0] as SensitiveUsageEntity;
        Assert.NotNull(entity);
        Assert.Equal("Confidential", entity.Name);
        Assert.Equal("This is sensitive content", entity.Description);
    }

    [Fact]
    public void OMessageEntity_WithUnknownOType_DeserializesAsOMessageEntity()
    {
        string json = """
        {
          "type": "message",
          "entities": [
            {
              "type": "https://schema.org/Message",
              "@context": "https://schema.org",
              "@type": "UnknownType"
            }
          ]
        }
        """;

        CoreActivity coreActivity = CoreActivity.FromJsonString(json);
        TeamsActivity activity = TeamsActivity.FromActivity(coreActivity);

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);

        var entity = activity.Entities[0];
        Assert.IsType<OMessageEntity>(entity);
        Assert.Equal("UnknownType", entity.OType);
    }

    [Fact]
    public void Fixture_CitationEntity_DeserializesWithClaims()
    {
        string json = """
        {
          "type": "message",
          "entities": [
            {
              "type": "https://schema.org/Message",
              "@context": "https://schema.org",
              "@type": "Message",
              "additionalType": ["AIGeneratedContent"],
              "citation": [
                {
                  "@type": "Claim",
                  "position": 1,
                  "appearance": {
                    "@type": "DigitalDocument",
                    "name": "Test Document",
                    "abstract": "Test abstract",
                    "url": "https://example.com/doc",
                    "encodingFormat": "application/vnd.microsoft.card.adaptive"
                  }
                }
              ]
            }
          ]
        }
        """;

        CoreActivity coreActivity = CoreActivity.FromJsonString(json);
        TeamsActivity activity = TeamsActivity.FromActivity(coreActivity);

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);

        var citationEntity = activity.Entities[0] as CitationEntity;
        Assert.NotNull(citationEntity);
        Assert.NotNull(citationEntity.AdditionalType);
        Assert.Contains("AIGeneratedContent", citationEntity.AdditionalType);
        Assert.NotNull(citationEntity.Citation);
        Assert.Single(citationEntity.Citation);
        Assert.Equal(1, citationEntity.Citation[0].Position);
        Assert.Equal("Test Document", citationEntity.Citation[0].Appearance.Name);
        Assert.Equal("Test abstract", citationEntity.Citation[0].Appearance.Abstract);
        Assert.Equal(EncodingFormats.AdaptiveCard, citationEntity.Citation[0].Appearance.EncodingFormat);
    }

    [Fact]
    public void CitationEntity_CopyConstructor_PreservesData()
    {
        var original = new CitationEntity();
        original.AdditionalType = ["AIGeneratedContent"];
        original.Citation = [
            new CitationClaim
            {
                Position = 1,
                Appearance = new CitationAppearanceDocument
                {
                    Name = "Doc",
                    Abstract = "Abstract"
                }
            }
        ];

        var copy = new CitationEntity(original);

        Assert.NotNull(copy.AdditionalType);
        Assert.Contains("AIGeneratedContent", copy.AdditionalType);
        Assert.NotNull(copy.Citation);
        Assert.Single(copy.Citation);
        Assert.Equal(1, copy.Citation[0].Position);
        Assert.Equal("Doc", copy.Citation[0].Appearance.Name);

        // Ensure it's a deep copy (modifying copy doesn't affect original)
        copy.AdditionalType.Add("NewType");
        Assert.Single(original.AdditionalType);
    }
}
