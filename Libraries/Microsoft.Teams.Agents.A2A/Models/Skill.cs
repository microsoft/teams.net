using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Models;

/// <summary>
/// Skills are a unit of capability that an agent can perform.
/// </summary>
public class Skill
{
    /// <summary>
    /// unique identifier for the agent's skill
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// human readable name of the skill
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public required string Name { get; set; }

    /// <summary>
    /// description of the skill - will be used by the client or a human
    /// as a hint to understand what the skill does.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonPropertyOrder(2)]
    public required string Description { get; set; }

    /// <summary>
    /// Set of tagwords describing classes of capabilities for this specific
    /// skill (e.g. "cooking", "customer support", "billing")
    /// </summary>
    [JsonPropertyName("tags")]
    [JsonPropertyOrder(3)]
    public IList<string> Tags { get; set; } = [];

    /// <summary>
    /// The set of example scenarios that the skill can perform.
    /// Will be used by the client as a hint to understand how the skill can be
    /// used. (e.g. "I need a recipe for bread")
    /// </summary>
    [JsonPropertyName("examples")]
    [JsonPropertyOrder(4)]
    public IList<string>? Examples { get; set; }

    /// <summary>
    /// The set of interaction modes that the skill supports
    /// (if different than the default)
    /// </summary>
    [JsonPropertyName("inputModes")]
    [JsonPropertyOrder(5)]
    public IList<string>? InputModes { get; set; }

    /// <summary>
    /// The set of interaction modes that the skill supports
    /// (if different than the default)
    /// </summary>
    [JsonPropertyName("outputModes")]
    [JsonPropertyOrder(6)]
    public IList<string>? OutputModes { get; set; }
}