// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Net;
using Microsoft.Teams.Core;

namespace Microsoft.Teams.Apps;

internal static class ReplyChainFallback
{
    /// <summary>
    /// Runs the reply chain call and, when the service answers HTTP 404, runs <paramref name="fallback"/> instead.
    /// When <paramref name="alsoOnBadRequest"/> is true, HTTP 400 runs <paramref name="fallback"/> as well.
    /// Any other failure propagates, and so does a 404 or a 400 when <paramref name="fallback"/> is null.
    /// </summary>
    internal static async Task<SendActivityResponse?> SendAsync(
        Func<Task<SendActivityResponse?>> replyChain,
        Func<Task<SendActivityResponse?>>? fallback,
        bool alsoOnBadRequest = false)
    {
        try
        {
            return await replyChain().ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (fallback is not null && (ex.StatusCode == HttpStatusCode.NotFound || (alsoOnBadRequest && ex.StatusCode == HttpStatusCode.BadRequest)))
        {
            return await fallback().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Whether the id is a plain non-zero number, the only kind of reply chain id that can be turned into a <c>;messageid=</c> suffix.
    /// </summary>
    internal static bool IsPlainNonZeroNumber(string id)
        => ulong.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out ulong parsed) && parsed != 0;
}
