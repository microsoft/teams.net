// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Auth;

public interface IHttpCredentials
{
    public Task<ITokenResponse> Resolve(IHttpClient client, string[] scopes, CancellationToken cancellationToken = default);
}