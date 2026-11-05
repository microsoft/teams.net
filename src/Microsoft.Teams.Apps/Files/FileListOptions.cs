// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// Options accepted by <see cref="FilesAccessor.ListAsync(FileListOptions, CancellationToken)"/> and <see cref="FilesAccessor.FirstAsync(FileListOptions, CancellationToken)"/>.
/// </summary>
public sealed class FileListOptions
{
    /// <summary>
    /// When set, only files with this style are returned.
    /// When omitted, both styles are returned.
    /// </summary>
    public AttachmentStyle? AttachmentStyle { get; init; }
}
