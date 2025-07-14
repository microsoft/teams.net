// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Apps;

public class Request
{
    public required IToken Token { get; set; }
    public required IContext<IActivity> Context { get; set; }
}