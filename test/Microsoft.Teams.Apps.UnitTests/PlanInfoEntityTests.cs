// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class PlanInfoEntityTests
{
    [Theory]
    [InlineData("start")]
    [InlineData("continue")]
    [InlineData("complete")]
    [InlineData("stop")]
    [InlineData("error")]
    public void PlanTypes_RoundTripThroughActivity(string planType)
    {
        MessageActivityInput message = new MessageActivityInput().AddEntity(
            new PlanInfoEntity { PlanType = new PlanType(planType) });

        JsonNode wire = JsonNode.Parse(message.ToJson())!;
        Assert.Equal("planInfo", wire["entities"]![0]!["type"]!.GetValue<string>());
        Assert.Equal(planType, wire["entities"]![0]!["planType"]!.GetValue<string>());
        Assert.Null(wire["entities"]![0]!["steps"]);

        TeamsActivity incoming = TeamsActivity.FromActivity(CoreActivity.FromJsonString(message.ToJson()));
        Assert.IsType<PlanInfoEntity>(Assert.Single(incoming.Entities!));
        Assert.Equal(new PlanType(planType), incoming.GetPlanInfo()!.PlanType);
    }

    [Theory]
    [InlineData("active")]
    [InlineData("proposed")]
    [InlineData("completed")]
    [InlineData("blocked")]
    [InlineData("error")]
    public void Steps_RoundTripWithSourcesAndSharedCitationIcon(string status)
    {
        PlanInfoEntity entity = new()
        {
            PlanType = PlanTypes.Continue,
            Steps =
            [
                new PlanStep
                {
                    Id = "research",
                    Version = 2,
                    Title = "Research",
                    Status = new PlanStepStatus(status),
                    Description = "Found the reference.",
                    Sources =
                    [
                        new StepSource
                        {
                            Title = "Reference",
                            Url = new Uri("https://example.com/reference"),
                            Icon = CitationIcons.MicrosoftWord
                        }
                    ]
                }
            ]
        };
        MessageActivityInput message = new MessageActivityInput().AddEntity(entity);
        JsonNode wire = JsonNode.Parse(message.ToJson())!;
        JsonNode step = wire["entities"]![0]!["steps"]![0]!;
        Assert.Equal("research", step["id"]!.GetValue<string>());
        Assert.Equal(2, step["version"]!.GetValue<int>());
        Assert.Equal(status, step["status"]!.GetValue<string>());
        Assert.Equal("Microsoft Word", step["sources"]![0]!["icon"]!.GetValue<string>());

        TeamsActivity incoming = TeamsActivity.FromActivity(CoreActivity.FromJsonString(message.ToJson()));
        PlanStep parsed = Assert.Single(incoming.GetPlanInfo()!.Steps!);
        Assert.Equal("research", parsed.Id);
        Assert.Equal(2, parsed.Version);
        Assert.Equal(new PlanStepStatus(status), parsed.Status);
        Assert.Equal("Found the reference.", parsed.Description);
        StepSource source = Assert.Single(parsed.Sources!);
        Assert.Equal(new Uri("https://example.com/reference"), source.Url);
        Assert.Equal(CitationIcons.MicrosoftWord, source.Icon);
    }

    [Fact]
    public void OptionalFields_AreOmittedAndEmptyStepsArePreserved()
    {
        PlanStep step = new() { Id = "one", Version = 1, Title = "One", Status = PlanStepStatuses.Active };
        JsonObject stepJson = JsonNode.Parse(JsonSerializer.Serialize(step))!.AsObject();
        Assert.False(stepJson.ContainsKey("description"));
        Assert.False(stepJson.ContainsKey("sources"));

        StepSource source = new() { Title = "Reference", Url = new Uri("https://example.com") };
        Assert.False(JsonNode.Parse(JsonSerializer.Serialize(source))!.AsObject().ContainsKey("icon"));

        PlanInfoEntity entity = new() { PlanType = PlanTypes.Start, Steps = [] };
        EntityList entities = [entity];
        Assert.Empty(JsonNode.Parse(JsonSerializer.Serialize(entities))![0]!["steps"]!.AsArray());
        entity.Steps = null;
        Assert.False(entities.ToJsonArray()![0]!.AsObject().ContainsKey("steps"));
    }

    [Fact]
    public void Entity_RoundTripsThroughSourceGeneratedContext()
    {
        PlanInfoEntity entity = new()
        {
            PlanType = PlanTypes.Complete,
            Steps = [new PlanStep { Id = "one", Version = 3, Title = "One", Status = PlanStepStatuses.Completed }]
        };
        string json = JsonSerializer.Serialize(entity, TeamsActivityInputJsonContext.Default.PlanInfoEntity);
        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Single(document.RootElement.EnumerateObject(), property => property.Name == "planType");
        Assert.Single(document.RootElement.EnumerateObject(), property => property.Name == "steps");
        PlanInfoEntity parsed = JsonSerializer.Deserialize(json, TeamsActivityInputJsonContext.Default.PlanInfoEntity)!;
        Assert.Equal(PlanTypes.Complete, parsed.PlanType);
        Assert.Equal(3, Assert.Single(parsed.Steps!).Version);

        string stepJson = JsonSerializer.Serialize(entity.Steps[0], TeamsActivityInputJsonContext.Default.PlanStep);
        Assert.Equal("one", JsonSerializer.Deserialize(stepJson, TeamsActivityInputJsonContext.Default.PlanStep)!.Id);
        StepSource source = new() { Title = "Source", Url = new Uri("https://example.com"), Icon = CitationIcons.Text };
        string sourceJson = JsonSerializer.Serialize(source, TeamsActivityInputJsonContext.Default.StepSource);
        Assert.Equal(CitationIcons.Text, JsonSerializer.Deserialize(sourceJson, TeamsActivityInputJsonContext.Default.StepSource)!.Icon);
    }

    [Fact]
    public void MissingRequiredFields_AreRejected()
    {
        Assert.Throws<JsonException>(() => EntityList.FromJsonArray(JsonNode.Parse("""[{"type":"planInfo"}]""")!.AsArray()));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PlanStep>("""{"id":"one","title":"One","status":"active"}"""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StepSource>("""{"title":"Source"}"""));
    }

    [Theory]
    [InlineData("message")]
    [InlineData("typing")]
    public void KnownPlanEntity_RejectsInvalidEnumInsteadOfFallingBack(string activityType)
    {
        string json = $$"""{"type":"{{activityType}}","entities":[{"type":"planInfo","planType":"unknown"}]}""";
        Assert.Throws<JsonException>(() => TeamsActivity.FromActivity(CoreActivity.FromJsonString(json)));
    }

    [Theory]
    [InlineData("""{"id":"one","version":0,"title":"One","status":"unknown"}""")]
    [InlineData("""{"id":"one","version":0,"title":"One","status":"active","sources":[{"title":"Source","url":"https://example.com","icon":"document"}]}""")]
    public void Step_RejectsInvalidStatusOrSourceIcon(string json)
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PlanStep>(json));
}
