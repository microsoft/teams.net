// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Backing store for <see cref="TurnState"/>. Implementations persist state documents keyed by
/// string. All members must be safe to call concurrently.
/// </summary>
public interface IStorage
{
    /// <summary>
    /// Reads the documents for the given keys. Keys with no stored document are omitted from the result.
    /// </summary>
    /// <param name="keys">The keys to read.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A map of key to <see cref="StoreItem"/> for keys that exist.</returns>
    Task<IReadOnlyDictionary<string, StoreItem>> ReadAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the given documents, overwriting any existing documents for the same keys.
    /// </summary>
    /// <param name="changes">A map of key to <see cref="StoreItem"/> to write.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task WriteAsync(IReadOnlyDictionary<string, StoreItem> changes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the documents for the given keys. Missing keys are ignored.
    /// </summary>
    /// <param name="keys">The keys to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default);
}
