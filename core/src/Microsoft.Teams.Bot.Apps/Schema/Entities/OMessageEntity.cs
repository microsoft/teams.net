// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema.Entities;

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
        get
        {
            if (!base.Properties.TryGetValue("additionalType", out object? value))
                return null;
            if (value is IList<string> list)
                return list;
            if (value is System.Text.Json.JsonElement je)
            {
                IList<string>? deserialized = je.Deserialize<IList<string>>();
                if (deserialized is not null)
                    base.Properties["additionalType"] = deserialized;
                return deserialized;
            }
            return null;
        }
        set => base.Properties["additionalType"] = value;
    }
}
