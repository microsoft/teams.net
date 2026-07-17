// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Plugins.AspNetCore;

public class AspNetCorePluginOptions
{
    /// <summary>
    /// Allow the Teams messaging endpoint to accept unauthenticated requests.
    /// This should only be enabled for local development.
    /// </summary>
    public bool DangerouslyAllowUnauthenticatedRequests { get; set; }

    [Obsolete("SkipAuth is deprecated. Use DangerouslyAllowUnauthenticatedRequests instead.")]
    public bool SkipAuth
    {
        get => DangerouslyAllowUnauthenticatedRequests;
        set => DangerouslyAllowUnauthenticatedRequests = value;
    }
}
