// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common.Http;

/// <summary>
/// Http Credential resolver used to fetch some access token.
/// </summary>
public interface IHttpCredentials
{
    public Task<ITokenResponse> Resolve(IHttpClient client, string[] scopes, CancellationToken cancellationToken = default);
}