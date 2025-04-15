using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Models;

/// <summary>
/// An AgentCard conveys key information:
/// - Overall details (version, name, description, uses)
/// - Skills: A set of capabilities the agent can perform
/// - Default modalities/content types supported by the agent.
/// - Authentication requirements
/// </summary>
public class Card
{
    /// <summary>
    /// Human readable name of the agent.
    /// (e.g. "Recipe Agent")
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(0)]
    public required string Name { get; set; }

    /// <summary>
    /// A human-readable description of the agent. Used to assist users and
    /// other agents in understanding what the agent can do.
    /// (e.g. "Agent that helps users with recipes and cooking.")
    /// </summary>
    [JsonPropertyName("description")]
    [JsonPropertyOrder(1)]
    public required string Description { get; set; }

    /// <summary>
    /// A URL to the address the agent is hosted at.
    /// </summary>
    [JsonPropertyName("url")]
    [JsonPropertyOrder(2)]
    public required string Url { get; set; }

    /// <summary>
    /// The service provider of the agent
    /// </summary>
    [JsonPropertyName("provider")]
    [JsonPropertyOrder(3)]
    public Provider? Provider { get; set; }

    /// <summary>
    /// The version of the agent - format is up to the provider. (e.g. "1.0.0")
    /// </summary>
    [JsonPropertyName("version")]
    [JsonPropertyOrder(4)]
    public required string Version { get; set; }

    /// <summary>
    /// A URL to documentation for the agent.
    /// </summary>
    [JsonPropertyName("documentationUrl")]
    [JsonPropertyOrder(5)]
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// Optional capabilities supported by the agent.
    /// </summary>
    [JsonPropertyName("capabilities")]
    [JsonPropertyOrder(6)]
    public IDictionary<string, bool> Capabilities { get; set; } = new Dictionary<string, bool>();

    /// <summary>
    /// Authentication requirements for the agent.
    /// Intended to match OpenAPI authentication structure.
    /// </summary>
    [JsonPropertyName("authentication")]
    [JsonPropertyOrder(7)]
    public required Authentication Authentication { get; set; }

    /// <summary>
    /// The set of interaction modes that the agent
    /// supports across all skills. This can be overridden per-skill.
    /// </summary>
    [JsonPropertyName("defaultInputModes")]
    [JsonPropertyOrder(8)]
    public IList<string> DefaultInputModes { get; set; } = [];

    /// <summary>
    /// The set of interaction modes that the agent
    /// supports across all skills. This can be overridden per-skill.
    /// </summary>
    [JsonPropertyName("defaultOutputModes")]
    [JsonPropertyOrder(9)]
    public IList<string> DefaultOutputModes { get; set; } = [];

    /// <summary>
    /// Skills are a unit of capability that an agent can perform.
    /// </summary>
    [JsonPropertyName("skills")]
    [JsonPropertyOrder(10)]
    public IList<Skill> Skills { get; set; } = [];
}

/// <summary>
/// The service provider of the agent
/// </summary>
public class Provider
{
    [JsonPropertyName("organization")]
    [JsonPropertyOrder(0)]
    public required string Organization { get; set; }

    [JsonPropertyName("url")]
    [JsonPropertyOrder(1)]
    public required string Url { get; set; }
}

/// <summary>
/// Authentication requirements for the agent.
/// Intended to match OpenAPI authentication structure.
/// </summary>
public class Authentication
{
    /// <summary>
    /// e.g. Basic, Bearer
    /// </summary>
    [JsonPropertyName("schemes")]
    [JsonPropertyOrder(0)]
    public IList<string> Schemes { get; set; } = [];

    /// <summary>
    /// credentials a client should use for private cards
    /// </summary>
    [JsonPropertyName("credentials")]
    [JsonPropertyOrder(1)]
    public string? Credentials { get; set; }
}