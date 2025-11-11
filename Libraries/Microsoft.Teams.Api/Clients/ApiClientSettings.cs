// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Api.Clients;

/// <summary>
/// Settings for API clients.
/// </summary>
public class ApiClientSettings
{
    /// <summary>
    /// The URL to use for managing user OAuth tokens.
    /// Specify this value if you are using a regional bot.
    /// For example: https://europe.token.botframework.com
    /// Default is https://token.botframework.com
    /// </summary>
    public string OAuthUrl { get; set; } = "https://token.botframework.com";

    /// <summary>
    /// Creates a new instance of ApiClientSettings with default values.
    /// </summary>
    public ApiClientSettings()
    {
    }

    /// <summary>
    /// Creates a new instance of ApiClientSettings with the specified OAuth URL.
    /// </summary>
    /// <param name="oauthUrl">The OAuth URL to use.</param>
    public ApiClientSettings(string oauthUrl)
    {
        OAuthUrl = oauthUrl;
    }

    /// <summary>
    /// Default API client settings.
    /// </summary>
    public static ApiClientSettings Default { get; } = new ApiClientSettings();

    /// <summary>
    /// Merges API client settings with environment variables and defaults.
    /// </summary>
    /// <param name="settings">Optional API client settings to merge.</param>
    /// <returns>Merged API client settings.</returns>
    public static ApiClientSettings Merge(ApiClientSettings? settings = null)
    {
        settings ??= new ApiClientSettings();

        // Check for environment variable override
        var envOAuthUrl = Environment.GetEnvironmentVariable("OAUTH_URL");

        return new ApiClientSettings
        {
            OAuthUrl = !string.IsNullOrEmpty(envOAuthUrl) ? envOAuthUrl : settings.OAuthUrl
        };
    }
}
