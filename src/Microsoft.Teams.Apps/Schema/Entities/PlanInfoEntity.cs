// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Utils;

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>Prompt-bound plan progress rendered by Teams.</summary>
[Experimental("ExperimentalTeamsPlan")]
[JsonConverter(typeof(PlanInfoEntityJsonConverter))]
public class PlanInfoEntity() : Entity("planInfo")
{
    /// <summary>The lifecycle update being sent.</summary>
    [JsonPropertyName("planType")]
    public required PlanType PlanType
    {
        get => Properties.Get<PlanType>("planType")
            ?? throw new InvalidOperationException("planType is required on PlanInfoEntity.");
        set
        {
            if (value?.Value is not ("start" or "continue" or "complete" or "stop" or "error"))
                throw new JsonException("Invalid planType.");
            Properties["planType"] = value;
        }
    }

    /// <summary>Optional step updates; omitted steps are left unchanged.</summary>
    [JsonPropertyName("steps")]
    public IList<PlanStep>? Steps
    {
        get => Properties.Get<IList<PlanStep>>("steps");
        set
        {
            if (value is null)
                Properties.Remove("steps");
            else
                Properties["steps"] = value;
        }
    }
}

internal sealed class PlanInfoEntityJsonConverter : JsonConverter<PlanInfoEntity>
{
    public override PlanInfoEntity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonObject json = JsonSerializer.Deserialize<JsonObject>(ref reader, options)
            ?? throw new JsonException("Expected a Plan info entity.");
        if (json["type"]?.Deserialize<string>(options) is string type && type != "planInfo")
            throw new JsonException("Expected entity type planInfo.");

        PlanInfoEntity entity = new()
        {
            PlanType = json["planType"]?.Deserialize<PlanType>(options)
                ?? throw new JsonException("planType is required on PlanInfoEntity."),
            Steps = json["steps"]?.Deserialize<IList<PlanStep>>(options),
            OType = json["@type"]?.Deserialize<string>(options),
            OContext = json["@context"]?.Deserialize<string>(options)
        };
        foreach (KeyValuePair<string, JsonNode?> property in json)
        {
            if (property.Key is not ("type" or "planType" or "steps" or "@type" or "@context"))
                entity.Properties[property.Key] = property.Value?.DeepClone();
        }
        return entity;
    }

    public override void Write(Utf8JsonWriter writer, PlanInfoEntity value, JsonSerializerOptions options)
    {
        // Entity properties are dictionary-backed; serialize each wire field only once.
        JsonObject json = new EntityList { value }.ToJsonArray()![0]!.AsObject();
        if (value.OType is not null) json["@type"] = value.OType;
        if (value.OContext is not null) json["@context"] = value.OContext;
        json.WriteTo(writer, options);
    }
}

/// <summary>A versioned step. Applications increment the version for every change.</summary>
[Experimental("ExperimentalTeamsPlan")]
public class PlanStep
{
    private PlanStepStatus _status = null!;

    /// <summary>Stable identifier within the plan.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>Application-managed, increment this value on every revision of this step.</summary>
    [JsonPropertyName("version")]
    public required int Version { get; set; }

    /// <summary>Short step title.</summary>
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    /// <summary>Current step status.</summary>
    [JsonPropertyName("status")]
    public required PlanStepStatus Status
    {
        get => _status;
        set
        {
            if (value?.Value is not ("active" or "proposed" or "completed" or "blocked" or "error"))
                throw new JsonException("Invalid Plan step status.");
            _status = value;
        }
    }

    /// <summary>Optional progress details.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>Optional sources supporting the step.</summary>
    [JsonPropertyName("sources")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<StepSource>? Sources { get; set; }
}

/// <summary>A source linked from a plan step.</summary>
[Experimental("ExperimentalTeamsPlan")]
public class StepSource
{
    /// <summary>Display title.</summary>
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    /// <summary>Source URL.</summary>
    [JsonPropertyName("url")]
    public required Uri Url { get; set; }

    /// <summary>Optional icon shared with message citations.</summary>
    [JsonPropertyName("icon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CitationIcon? Icon { get; set; }
}

/// <summary>Plan lifecycle values.</summary>
[JsonConverter(typeof(StringEnumJsonConverter<PlanType>))]
public class PlanType(string value) : StringEnum(value)
{
    /// <summary>Start a plan.</summary>
    public static readonly PlanType Start = new("start");
    /// <summary>Continue a plan.</summary>
    public static readonly PlanType Continue = new("continue");
    /// <summary>Complete a plan with a final message.</summary>
    public static readonly PlanType Complete = new("complete");
    /// <summary>Stop a plan.</summary>
    public static readonly PlanType Stop = new("stop");
    /// <summary>Report a plan error.</summary>
    public static readonly PlanType Error = new("error");
}

/// <summary>Known plan lifecycle values.</summary>
public static class PlanTypes
{
    /// <summary>Start a plan.</summary>
    public static PlanType Start => PlanType.Start;
    /// <summary>Continue a plan.</summary>
    public static PlanType Continue => PlanType.Continue;
    /// <summary>Complete a plan.</summary>
    public static PlanType Complete => PlanType.Complete;
    /// <summary>Stop a plan.</summary>
    public static PlanType Stop => PlanType.Stop;
    /// <summary>Report a plan error.</summary>
    public static PlanType Error => PlanType.Error;
}

/// <summary>Plan step status values.</summary>
[JsonConverter(typeof(StringEnumJsonConverter<PlanStepStatus>))]
public class PlanStepStatus(string value) : StringEnum(value)
{
    /// <summary>Work is in progress.</summary>
    public static readonly PlanStepStatus Active = new("active");
    /// <summary>Work is proposed.</summary>
    public static readonly PlanStepStatus Proposed = new("proposed");
    /// <summary>Work is complete.</summary>
    public static readonly PlanStepStatus Completed = new("completed");
    /// <summary>Work is blocked.</summary>
    public static readonly PlanStepStatus Blocked = new("blocked");
    /// <summary>Work failed.</summary>
    public static readonly PlanStepStatus Error = new("error");
}

/// <summary>Known plan step statuses.</summary>
public static class PlanStepStatuses
{
    /// <summary>Work is in progress.</summary>
    public static PlanStepStatus Active => PlanStepStatus.Active;
    /// <summary>Work is proposed.</summary>
    public static PlanStepStatus Proposed => PlanStepStatus.Proposed;
    /// <summary>Work is complete.</summary>
    public static PlanStepStatus Completed => PlanStepStatus.Completed;
    /// <summary>Work is blocked.</summary>
    public static PlanStepStatus Blocked => PlanStepStatus.Blocked;
    /// <summary>Work failed.</summary>
    public static PlanStepStatus Error => PlanStepStatus.Error;
}
