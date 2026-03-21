// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.DevTools.Models;

namespace Microsoft.Teams.Bot.DevTools;

/// <summary>
/// Configuration settings for DevTools.
/// Bound from the "DevTools" configuration section.
/// </summary>
public class DevToolsSettings
{
    /// <summary>
    /// Custom pages to display in the DevTools UI.
    /// </summary>
    public IList<Page> Pages { get; } = [];
}
