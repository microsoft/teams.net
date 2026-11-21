// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps;

/// <summary>
/// Options for a single reply call.
/// </summary>
/// <remarks>
/// This API is in preview and may change in the future. Diagnostic: ExperimentalTeamsThreading
/// </remarks>
public sealed class ReplyOptions
{
    /// <summary>
    /// The reply mode for this call.
    /// Overrides the app's <see cref="TeamsBotApplicationOptions.ReplyMode"/> setting.
    /// When null, the app's setting is used, which defaults to <see cref="ReplyMode.Combined"/>.
    /// </summary>
    public ReplyMode? Mode { get; init; }
}
