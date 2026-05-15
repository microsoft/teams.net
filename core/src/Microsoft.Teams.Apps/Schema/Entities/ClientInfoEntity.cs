// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Client info entity.
/// </summary>
public class ClientInfoEntity : Entity
{
    /// <summary>
    /// Creates a new instance of <see cref="ClientInfoEntity"/>.
    /// </summary>
    public ClientInfoEntity() : base("clientInfo")
    {
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="ClientInfoEntity"/> class with specified client information.
    /// </summary>
    /// <param name="platform">The platform identifier (e.g., "web", "desktop", "mobile").</param>
    /// <param name="country">The country code (e.g., "US", "GB").</param>
    /// <param name="timezone">The time zone identifier (e.g., "America/New_York").</param>
    /// <param name="locale">The locale identifier (e.g., "en-US", "fr-FR").</param>
    public ClientInfoEntity(string platform, string country, string timezone, string locale) : base("clientInfo")
    {
        Locale = locale;
        Country = country;
        Platform = platform;
        Timezone = timezone;
    }

    /// <summary>
    /// Gets or sets the locale information.
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale
    {
        get => base.Properties.TryGetValue("locale", out object? value) ? value?.ToString() : null;
        set => base.Properties["locale"] = value;
    }

    /// <summary>
    /// Gets or sets the country information.
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country
    {
        get => base.Properties.TryGetValue("country", out object? value) ? value?.ToString() : null;
        set => base.Properties["country"] = value;
    }

    /// <summary>
    /// Gets or sets the platform information.
    /// </summary>
    [JsonPropertyName("platform")]
    public string? Platform
    {
        get => base.Properties.TryGetValue("platform", out object? value) ? value?.ToString() : null;
        set => base.Properties["platform"] = value;
    }

    /// <summary>
    /// Gets or sets the timezone information.
    /// </summary>
    [JsonPropertyName("timezone")]
    public string? Timezone
    {
        get => base.Properties.TryGetValue("timezone", out object? value) ? value?.ToString() : null;
        set => base.Properties["timezone"] = value;
    }
}

/// <summary>
/// Client info entity extension methods.
/// </summary>
public static class ClientInfoEntityExtensions
{
    /// <summary>
    /// Gets the first client information entity from the activity.
    /// </summary>
    public static ClientInfoEntity? GetClientInfo(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return null;
        }

        return activity.Entities.FirstOrDefault(e => e is ClientInfoEntity) as ClientInfoEntity;
    }
}
