// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// Follows redirects for the file downloader's client, which has automatic redirects turned off.
/// <para>Storage answers with a 302 to the host actually holding the bytes, so redirects have to be followed. They must not be followed <em>down</em> to plaintext: an <c>https</c> to <c>http</c> hop would put the file's bytes on the wire in the clear.</para>
/// <para>The built-in follower drops <c>Authorization</c> on every hop, the original host included, so a Bot API redirect back to its own host would fail as unauthorized. This follower keeps the caller's <c>Authorization</c> while the scheme, host, and port stay those of the original request, and drops it for good once a hop leaves them.</para>
/// </summary>
internal sealed class FileRedirectHandler : DelegatingHandler
{
    internal const int MaxRedirects = 21;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Uri origin = request.RequestUri ?? throw new InvalidOperationException("cannot download file: the request has no URL");
        bool sameOrigin = true;
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        for (int hops = 0; IsRedirect(response.StatusCode) && response.Headers.Location is { } location; hops++)
        {
            if (hops == MaxRedirects)
            {
                response.Dispose();
                throw new HttpRequestException("cannot download file: too many redirects");
            }

            Uri current = response.RequestMessage?.RequestUri ?? origin;
            Uri next = location.IsAbsoluteUri ? location : new Uri(current, location);

            if (string.Equals(current.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) && !string.Equals(next.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
            {
                response.Dispose();
                throw new InvalidOperationException(
                    $"cannot download file: a redirect destination must use https, got '{next.Scheme}://{next.Host}'. The file's bytes would cross that hop in the clear.");
            }

            sameOrigin = sameOrigin && Uri.Compare(next, origin, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;
            response.Dispose();

#pragma warning disable CA2000 // the returned response references this request as its RequestMessage
            HttpRequestMessage hop = new(HttpMethod.Get, next);
#pragma warning restore CA2000
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                if (!string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    hop.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (sameOrigin)
            {
                hop.Headers.Authorization = request.Headers.Authorization;
            }

            response = await base.SendAsync(hop, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    private static bool IsRedirect(HttpStatusCode status) => status is HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
}
