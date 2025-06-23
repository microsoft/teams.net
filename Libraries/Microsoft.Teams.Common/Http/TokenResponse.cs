// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common.Http;

public interface ITokenResponse
{
    public string TokenType { get; }
    public int? ExpiresIn { get; }
    public string AccessToken { get; }
}