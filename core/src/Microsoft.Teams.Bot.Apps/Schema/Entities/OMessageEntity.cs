// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// OMessage entity.
/// </summary>
public class OMessageEntity : Entity
{

    /// <summary>
    /// Creates a new instance of <see cref="OMessageEntity"/>.
    /// </summary>
    public OMessageEntity() : base("https://schema.org/Message")
    {
        OType = "Message";
        OContext = "https://schema.org";
    }
    /// <summary>
    /// Gets or sets the additional type.
    /// </summary>
    [JsonPropertyName("additionalType")]
    public IList<string>? AdditionalType
    {
        get => base.Properties.TryGetValue("additionalType", out object? value) ? value as IList<string> : null;
        set => base.Properties["additionalType"] = value;
    }
}
