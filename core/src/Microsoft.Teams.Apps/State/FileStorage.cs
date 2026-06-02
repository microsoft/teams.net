// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// File-backed <see cref="IStorage"/> that persists each state document as a JSON file under a root
/// directory. Storage keys are percent-encoded into flat file names — a key like
/// <c>"msteams/conversations/19:abc"</c> maps to a single file — using the same encoding as the Node
/// (<c>encodeURIComponent</c>) and Python (<c>urllib.parse.quote</c>) SDKs, so documents are
/// cross-runtime compatible. Each file holds the bare values document (no .NET-specific envelope).
/// </summary>
/// <remarks>
/// Writes are atomic (write to a temp file, then rename over the target), so a concurrent reader never
/// observes a partially written document; concurrent writes to the same key are last-write-wins.
/// Intended for single-instance and development scenarios — use a distributed backend (e.g. Redis) for
/// multi-instance deployments.
/// </remarks>
public sealed class FileStorage : IStorage
{
    private readonly string _rootDirectory;

    /// <summary>Initializes a new <see cref="FileStorage"/> rooted at <paramref name="rootDirectory"/>.</summary>
    /// <param name="rootDirectory">The directory under which state documents are stored. Created if it does not exist.</param>
    public FileStorage(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(rootDirectory);
        _rootDirectory = rootDirectory;
        Directory.CreateDirectory(rootDirectory);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, StoreItem>> ReadAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        Dictionary<string, StoreItem> result = [];
        foreach (string key in keys)
        {
            string path = PathFor(key);
            if (!File.Exists(path))
            {
                continue;
            }

            string json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            result[key] = new StoreItem
            {
                Values = StateSerializer.Deserialize(json),
                ETag = File.GetLastWriteTimeUtc(path).Ticks.ToString(CultureInfo.InvariantCulture),
            };
        }

        return result;
    }

    /// <inheritdoc />
    public async Task WriteAsync(IReadOnlyDictionary<string, StoreItem> changes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changes);

        foreach ((string key, StoreItem item) in changes)
        {
            string path = PathFor(key);
            string json = StateSerializer.Serialize(item.Values);

            // Write to a temp file in the same directory, then atomically rename over the target so a
            // concurrent reader never sees a partially written document.
            string tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
            try
            {
                await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
                File.Move(tempPath, path, overwrite: true);
            }
            catch
            {
                TryDelete(tempPath);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        foreach (string key in keys)
        {
            // File.Delete is a no-op when the file is already gone (the root directory always exists).
            File.Delete(PathFor(key));
        }

        return Task.CompletedTask;
    }

    private string PathFor(string key) => Path.Combine(_rootDirectory, Uri.EscapeDataString(key) + ".json");

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Best-effort cleanup of a temp file; another operation may have already removed it.
        }
    }
}
