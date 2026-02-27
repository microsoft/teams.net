// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Apps.Extensions;

public class TeamsSettings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TenantId { get; set; }

    /// <summary>
    /// The Entra ID login endpoint, following the Microsoft Identity Web configuration schema.
    /// Override this for sovereign clouds (e.g. "https://login.microsoftonline.us" for US Gov).
    /// </summary>
    public string? Instance { get; set; }

    public bool Empty
    {
        get { return ClientId == "" || ClientSecret == ""; }
    }

    public AppOptions Apply(AppOptions? options = null)
    {
        options ??= new AppOptions();

        if (ClientId is not null && ClientSecret is not null && !Empty)
        {
            var credentials = new ClientCredentials(ClientId, ClientSecret, TenantId);

            if (Instance is not null)
            {
                credentials.Instance = Instance;
            }

            options.Credentials = credentials;
        }

        return options;
    }
}