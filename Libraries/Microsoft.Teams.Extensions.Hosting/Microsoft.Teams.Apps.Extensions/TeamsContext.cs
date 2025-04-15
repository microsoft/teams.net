using System.Diagnostics.CodeAnalysis;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Apps.Extensions;

public class TeamsContext
{
    [AllowNull]
    public IToken Token { get; set; }

    [AllowNull]
    public IContext<IActivity> Activity { get; set; }

    public Response? Response { get; set; }
}