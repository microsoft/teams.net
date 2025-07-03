// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Plugins.AspNetCore;

public interface IAspNetCorePlugin : IPlugin
{
    public IApplicationBuilder Configure(IApplicationBuilder builder);
}