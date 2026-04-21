// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.Apps.Auth;

/// <summary>
/// Options for the OAuth sign-in flow.
/// </summary>
public class OAuthOptions
{
    /// <summary>
    /// The OAuth connection name to use. If null, uses the default registered connection.
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// The text displayed on the OAuthCard.
    /// </summary>
    public string OAuthCardText { get; set; } = "Please Sign In";

    /// <summary>
    /// The text displayed on the sign-in button.
    /// </summary>
    public string SignInButtonText { get; set; } = "Sign In";
}
