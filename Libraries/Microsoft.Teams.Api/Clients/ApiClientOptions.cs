// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Api.Clients;

/// <summary>
/// Options for API clients.
/// </summary>
public class ApiClientOptions
{
    /// <summary>
    /// The URL to use for managing user OAuth tokens.
    /// Specify this value if you are using a regional bot.
    /// For example: https://europe.token.botframework.com
    /// Default is https://token.botframework.com
    /// </summary>
    public string OAuthUrl { get; set; } = "https://token.botframework.com";

    /// <summary>
    /// Creates a new instance of ApiClientOptions with default values.
    /// </summary>
    public ApiClientOptions()
    {
    }

    /// <summary>
    /// Creates a new instance of ApiClientOptions with the specified OAuth URL.
    /// </summary>
    /// <param name="oauthUrl">The OAuth URL to use.</param>
    public ApiClientOptions(string oauthUrl)
    {
        OAuthUrl = oauthUrl;
    }

    /// <summary>
    /// Default API client options.
    /// </summary>
    public static ApiClientOptions Default { get; } = new ApiClientOptions();

    /// <summary>
    /// Merges API client options with environment variables and defaults.
    /// </summary>
    /// <param name="options">Optional API client options to merge.</param>
    /// <returns>Merged API client options.</returns>
    public static ApiClientOptions Merge(ApiClientOptions? options = null)
    {
        options ??= new ApiClientOptions();

        // Check for environment variable override
        var envOAuthUrl = Environment.GetEnvironmentVariable("OAUTH_URL");

        return new ApiClientOptions
        {
            OAuthUrl = !string.IsNullOrEmpty(envOAuthUrl) ? envOAuthUrl : options.OAuthUrl
        };
    }
}
