// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps;

/// <summary>
/// Options for creating a <see cref="TeamsStreamingWriter"/>.
/// </summary>
/// <param name="ReplyChain">When set, the first message of the stream is created as a reply in this reply chain (thread).</param>
internal sealed record TeamsStreamingWriterOptions(ReplyChainPlacement? ReplyChain = null);
