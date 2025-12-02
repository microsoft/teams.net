// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common.Http;

public interface IHttpCredentials
{
    public Task<ITokenResponse> Resolve(ICustomHttpClient client, string[] scopes, CancellationToken cancellationToken = default);
}