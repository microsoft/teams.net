// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common.Http;

public interface IHttpClientFactory
{
    public IHttpClient CreateClient();
    public IHttpClient CreateClient(string name);
}