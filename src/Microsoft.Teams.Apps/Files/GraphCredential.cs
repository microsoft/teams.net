// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// Supplies a bearer token for Graph, and names the identity it belongs to.
/// <para>Resolved at fetch time rather than stored on the file handle, so a handle stays inert and a token is never acquired before it is needed. Returning <c>null</c> for the token means no credential is available, which surfaces as a <see cref="FileCredentialException"/> before any request is made.</para>
/// </summary>
public sealed class GraphCredential
{
    private readonly Func<CancellationToken, Task<string?>> _token;

    /// <summary>Initializes a new instance of the <see cref="GraphCredential"/> class.</summary>
    /// <param name="actor">Which identity this credential belongs to.</param>
    /// <param name="token">Resolves the bearer token, or <c>null</c> when no credential is available.</param>
    /// <param name="baseUrlRoot">Graph host root, e.g. <c>https://graph.microsoft.com</c>. The API version is appended at the point of use. Carried here so a new code path cannot wire the token through and forget its destination.</param>
    public GraphCredential(FileActor actor, Func<CancellationToken, Task<string?>> token, Uri? baseUrlRoot = null)
    {
        Actor = actor;
        _token = token ?? throw new ArgumentNullException(nameof(token));
        BaseUrlRoot = baseUrlRoot;
    }

    /// <summary>Which identity this credential belongs to.</summary>
    public FileActor Actor { get; }

    /// <summary>Graph host root, e.g. <c>https://graph.microsoft.com</c>. The API version is appended at the point of use.</summary>
    public Uri? BaseUrlRoot { get; }

    /// <summary>Resolve the bearer token, or <c>null</c> when no credential is available.</summary>
    /// <param name="cancellationToken">A token to cancel the acquisition.</param>
    public Task<string?> GetTokenAsync(CancellationToken cancellationToken = default) => _token(cancellationToken);
}
