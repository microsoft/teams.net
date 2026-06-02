// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// A persisted state document: the values for a single scope plus an optional concurrency tag.
/// </summary>
public sealed class StoreItem
{
    /// <summary>
    /// The state values for a single scope, keyed by name. Values may be primitives, strings,
    /// small POCOs, or <see cref="System.Text.Json.JsonElement"/> instances (as read back from storage).
    /// </summary>
    public IDictionary<string, object?> Values { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Optional concurrency token. Reserved for future optimistic-concurrency support; v1 storage
    /// providers use last-write-wins.
    /// </summary>
    public string? ETag { get; set; }
}
