// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.Core;


/// <summary>
/// Result object for GetTokenStatus API call.
/// </summary>
public class GetTokenStatusResult
{
    /// <summary>
    /// The connection name associated with the token.
    /// </summary>
    public string? ConnectionName { get; set; }
    /// <summary>
    ///  Indicates whether a token is available.
    /// </summary>
    public bool? HasToken { get; set; }
    /// <summary>
    /// The display name of the service provider.
    /// </summary>
    public string? ServiceProviderDisplayName { get; set; }
}

/// <summary>
/// Result object for GetToken API call.
/// </summary>
public class GetTokenResult
{
    /// <summary>
    /// The connection name associated with the token.
    /// </summary>
    public string? ConnectionName { get; set; }
    /// <summary>
    /// The token string.
    /// </summary>
    public string? Token { get; set; }
}

/// <summary>
/// SignIn resource object.
/// </summary>
public class GetSignInResourceResult
{
    /// <summary>
    /// The link for signing in.
    /// </summary>
    public string? SignInLink { get; set; }
    /// <summary>
    /// The resource for token post.
    /// </summary>
    public TokenPostResource? TokenPostResource { get; set; }

    /// <summary>
    /// The token exchange resources.
    /// </summary>
    public TokenExchangeResource? TokenExchangeResource { get; set; }
}
/// <summary>
/// Token post resource object.
/// </summary>
public class TokenPostResource
{
    /// <summary>
    /// The URL to which the token should be posted.
    /// </summary>
    public Uri? SasUrl { get; set; }
}

/// <summary>
/// Token exchange resource object.
/// </summary>
public class TokenExchangeResource
{
    /// <summary>
    /// ID of the token exchange resource.
    /// </summary>
    public string? Id { get; set; }
    /// <summary>
    /// Provider ID of the token exchange resource.
    /// </summary>
    public string? ProviderId { get; set; }
    /// <summary>
    /// URI of the token exchange resource.
    /// </summary>
    public Uri? Uri { get; set; }
}
