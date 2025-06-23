// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Plugins.AspNetCore.DevTools.Models;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;

public class TeamsDevToolsSettings
{
    public IList<Page> Pages { get; set; } = [];
}