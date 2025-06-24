// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Apps.Extensions;

public class TeamsContext
{
    public IToken Token { get; set; }
    public IContext<IActivity> Activity { get; set; }
    public Response? Response { get; set; }
}