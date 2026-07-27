// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Plugins.AspNetCore;

internal sealed class UnauthenticatedToken(string? serviceUrl) : IToken
{
    public string? AppId => null;
    public string? AppDisplayName => null;
    public string? TenantId => null;
    public string ServiceUrl { get; } = serviceUrl ?? string.Empty;
    public CallerType From => CallerType.Azure;
    public string FromId => "urn:botframework:azure";
    public DateTime? Expiration => null;
    public bool IsExpired => false;
    public IEnumerable<string> Scopes => [];
    public override string ToString() => string.Empty;
}
