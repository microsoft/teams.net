// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.BotApps.Schema.Entities;


/// <summary>
/// Extension methods for Activity to handle client info.
/// </summary>
public static class ActivityClientInfoExtensions
{
    /// <summary>
    /// Adds a client info to the activity.
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="platform"></param>
    /// <param name="country"></param>
    /// <param name="timeZone"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    public static ClientInfoEntity AddClientInfo(this TeamsActivity activity, string platform, string country, string timeZone, string locale)
    {
        ArgumentNullException.ThrowIfNull(activity);

        ClientInfoEntity clientInfo = new(platform, country, timeZone, locale);
        activity.Entities ??= new();
        activity.Entities.Add(clientInfo);
        activity.Rebase();
        return clientInfo;
    }

    /// <summary>
    /// Gets the client info from the activity's entities.
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    public static ClientInfoEntity? GetClientInfo(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return null;
        }
        ClientInfoEntity? clientInfo = activity.Entities.FirstOrDefault(e => e is ClientInfoEntity) as ClientInfoEntity;

        return clientInfo;
    }
}

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
        ToProperties();
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="ClientInfoEntity"/> class with specified parameters.
    /// </summary>
    /// <param name="platform"></param>
    /// <param name="country"></param>
    /// <param name="timezone"></param>
    /// <param name="locale"></param>
    public ClientInfoEntity(string platform, string country, string timezone, string locale) : base("clientInfo")
    {
        Locale = locale;
        Country = country;
        Platform = platform;
        Timezone = timezone;
        ToProperties();
    }
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

    /// <summary>
    /// Adds custom fields as properties.
    /// </summary>
    public override void ToProperties()
    {
        base.Properties.Add("locale", Locale);
        base.Properties.Add("country", Country);
        base.Properties.Add("platform", Platform);
        base.Properties.Add("timezone", Timezone);
    }
}
