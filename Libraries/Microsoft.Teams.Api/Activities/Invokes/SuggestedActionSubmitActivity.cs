// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    [Experimental("ExperimentalTeamsSuggestedAction")]
    public static readonly Name SuggestedActionSubmit = new("suggestedActions/submit");

    [Experimental("ExperimentalTeamsSuggestedAction")]
    public bool IsSuggestedActionSubmit => SuggestedActionSubmit.Equals(Value);
}

/// <summary>
/// Sent when the user clicks a suggested action of type <c>Action.Submit</c>.
/// The structured payload authored on the suggested action is delivered via <see cref="InvokeActivity.Value"/>.
/// </summary>
[Experimental("ExperimentalTeamsSuggestedAction")]
public class SuggestedActionSubmitActivity() : InvokeActivity(Name.SuggestedActionSubmit)
{
}
