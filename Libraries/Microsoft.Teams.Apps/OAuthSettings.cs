// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Clients;

/// <summary>
/// OAuth configuration settings for the application.
/// </summary>
public class OAuthSettings
{
    /// <summary>
    /// The default OAuth connection name to use for authentication.
    /// Defaults to "graph".
    /// </summary>
    public string DefaultConnectionName { get; set; }

    /// <summary>
    /// API client settings used for overriding.
    /// </summary>
    public ApiClientSettings? ApiClientSettings { get; set; }

    /// <summary>
    /// Creates a new instance of OAuthSettings with the specified connection name.
    /// </summary>
    /// <param name="connectionName">The default OAuth connection name. Defaults to "graph".</param>
    public OAuthSettings(string? connectionName = "graph")
    {
        DefaultConnectionName = connectionName ?? "graph";
    }
}