// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// The Bot API token for reading inline images, and the identity it belongs to.
/// <para>It is the turn's API client token, so an agentic identity reads as itself.
/// Resolved at fetch time, so a turn that never downloads an inline image never acquires one.</para>
/// </summary>
public sealed class BotApiCredential
{
    private readonly Func<CancellationToken, Task<string?>> _token;

    /// <summary>Initializes a new instance of the <see cref="BotApiCredential"/> class.</summary>
    /// <param name="actor">Which identity this credential belongs to.</param>
    /// <param name="token">Resolves the bearer token, or <c>null</c> when no credential is available.</param>
    /// <param name="serviceUrl">The activity's <c>serviceUrl</c>; the token is only sent to URLs under it.</param>
    public BotApiCredential(FileActor actor, Func<CancellationToken, Task<string?>> token, Uri? serviceUrl = null)
    {
        Actor = actor;
        _token = token ?? throw new ArgumentNullException(nameof(token));
        ServiceUrl = serviceUrl;
    }

    /// <summary>Which identity this credential belongs to.</summary>
    public FileActor Actor { get; }

    /// <summary>
    /// The activity's <c>serviceUrl</c>, the base the turn's replies are sent to.
    /// <para>Carried with the token so the token is never sent without its allowed destination.
    /// An inline image under a different URL is refused before the token is resolved.
    /// When absent, every inline image is refused.</para>
    /// </summary>
    public Uri? ServiceUrl { get; }

    /// <summary>Resolve the bearer token, or <c>null</c> when no credential is available.</summary>
    /// <param name="cancellationToken">A token to cancel the acquisition.</param>
    public Task<string?> GetTokenAsync(CancellationToken cancellationToken = default) => _token(cancellationToken);
}
