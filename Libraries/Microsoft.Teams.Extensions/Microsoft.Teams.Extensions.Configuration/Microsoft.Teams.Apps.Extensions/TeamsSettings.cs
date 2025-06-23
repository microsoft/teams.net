// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Apps.Extensions;

public class TeamsSettings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TenantId { get; set; }

    public bool Empty { 
        get { return ClientId != "" && ClientSecret != ""; }
    }

    public AppOptions Apply(AppOptions? options = null)
    {
        options ??= new AppOptions();

        if (ClientId is not null && ClientSecret is not null && !Empty)
        {
            options.Credentials = new ClientCredentials(ClientId, ClientSecret, TenantId);
        }

        return options;
    }

    public bool IsNotEmpty() {
        return ClientId != "" && ClientSecret != "";
    }
}