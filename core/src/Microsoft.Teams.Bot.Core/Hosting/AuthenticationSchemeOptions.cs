// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.Core.Hosting;

/// <summary>
/// Options for determining which authentication scheme to use.
/// </summary>
internal sealed class AuthenticationSchemeOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to use Agent authentication (true) or Bot authentication (false).
    /// </summary>
    public bool UseAgentAuth { get; set; }

    /// <summary>
    /// Gets or sets the scope value used to determine the authentication scheme.
    /// </summary>
    public string Scope { get; set; } = "https://api.botframework.com/.default";
}
