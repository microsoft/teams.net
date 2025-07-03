// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common.Http;

public interface IHttpCredentialsFactory
{
    public IHttpCredentials? GetCredentials();
    public Task<IHttpCredentials?> GetCredentialsAsync();
}