// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common.Http;

public interface ICustomHttpClientFactory
{
    public ICustomHttpClient CreateClient();
    public ICustomHttpClient CreateClient(string name);
}