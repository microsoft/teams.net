// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Schema.Entities;

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
        get => base.Properties.Get<IList<string>>("additionalType");
        set => base.Properties["additionalType"] = value;
    }
}

/// <summary>
/// OMessage entity extension methods.
/// </summary>
public static class OMessageEntityExtensions
{
    /// <summary>
    /// Gets the first message entity from the activity.
    /// </summary>
    public static OMessageEntity? GetMessageEntity(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return null;
        }

        return activity.Entities.FirstOrDefault(e => e.Type == "https://schema.org/Message" && e is OMessageEntity) as OMessageEntity;
    }
}
