// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.BotApps.Schema.Entities;

/// <summary>
/// Client info entity.
/// </summary>
public class ClientInfoEntity : Entity
{
    /// <summary>
    /// Creates a new instance of <see cref="ClientInfoEntity"/>.
    /// </summary>
    public ClientInfoEntity() : base("clientInfo") { }

    /// <summary>
    /// Gets or sets the locale information.
    /// </summary>
    [JsonPropertyName("locale")] public string? Locale { get; set; }

    /// <summary>
    /// Gets or sets the country information.
    /// </summary>
    [JsonPropertyName("country")] public string? Country { get; set; }

    /// <summary>
    /// Gets or sets the platform information.
    /// </summary>
    [JsonPropertyName("platform")] public string? Platform { get; set; }

    /// <summary>
    /// Gets or sets the timezone information.
    /// </summary>
    [JsonPropertyName("timezone")] public string? Timezone { get; set; }
}
