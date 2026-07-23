// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Core;

namespace Microsoft.Teams.Core.Hosting;

internal sealed class AuthenticationNotConfiguredHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.Fail("Authentication not configured"));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Logger.AuthenticationNotConfigured(Scheme.Name);

        await Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Authentication not configured"
        ).ExecuteAsync(Context).ConfigureAwait(false);
    }
}
