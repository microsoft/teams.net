// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

public class OAuthSettings(string? connectionName = "graph")
{
    public string DefaultConnectionName { get; set; } = connectionName ?? "graph";
}