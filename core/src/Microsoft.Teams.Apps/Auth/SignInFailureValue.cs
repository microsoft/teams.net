// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Auth;

/// <summary>
/// Value payload of the signin/failure invoke activity.
/// Sent by the Teams client when SSO token exchange fails client-side.
/// </summary>
/// <remarks>
/// Known failure codes:
/// <list type="bullet">
/// <item><term>installappfailed</term><description>Failed to install the app in the user's personal scope.</description></item>
/// <item><term>authrequestfailed</term><description>The SSO auth request failed after app installation.</description></item>
/// <item><term>installedappnotfound</term><description>The bot app is not installed for the user or group chat.</description></item>
/// <item><term>invokeerror</term><description>A generic error occurred during the SSO invoke flow.</description></item>
/// <item><term>resourcematchfailed</term><description>The token exchange resource URI does not match the Application ID URI in the Entra app's "Expose an API" section.</description></item>
/// <item><term>oauthcardnotvalid</term><description>The bot's OAuthCard could not be parsed.</description></item>
/// <item><term>tokenmissing</term><description>AAD token acquisition failed.</description></item>
/// <item><term>userconsentrequired</term><description>The user needs to consent (usually handled via OAuth card fallback).</description></item>
/// <item><term>interactionrequired</term><description>User interaction is required (usually handled via OAuth card fallback).</description></item>
/// </list>
/// </remarks>
public class SignInFailureValue
{
    /// <summary>
    /// The failure code identifying the type of SSO failure.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// A human-readable description of the failure.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
