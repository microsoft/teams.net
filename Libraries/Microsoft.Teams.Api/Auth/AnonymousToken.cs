// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Api.Auth;

/// <summary>
/// A fallback token used when no authentication is provided (e.g., skipAuth mode).
/// Mirrors the behavior of Python and TypeScript SDKs.
/// </summary>
public class AnonymousToken : IToken
{
    public string? AppId => string.Empty;

    public string? AppDisplayName => string.Empty;

    public string? TenantId => string.Empty;

    public string ServiceUrl { get; }

    public CallerType From => CallerType.Azure;

    public string FromId => string.Empty;

    public DateTime? Expiration => null;

    public bool IsExpired => false;

    public IEnumerable<string> Scopes => [];

    public AnonymousToken(string serviceUrl)
    {
        // Ensure serviceUrl has trailing slash for consistency
        ServiceUrl = serviceUrl.EndsWith('/') ? serviceUrl : serviceUrl + '/';
    }

    public override string ToString() => string.Empty;
}
