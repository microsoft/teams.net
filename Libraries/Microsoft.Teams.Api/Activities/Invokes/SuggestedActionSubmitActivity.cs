// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public static readonly Name SuggestedActionSubmit = new("suggestedAction/submit");
    public bool IsSuggestedActionSubmit => SuggestedActionSubmit.Equals(Value);
}

/// <summary>
/// Sent when the user clicks a suggested action of type <c>Action.Submit</c>.
/// The structured payload authored on the suggested action is delivered via <see cref="InvokeActivity.Value"/>.
/// </summary>
public class SuggestedActionSubmitActivity() : InvokeActivity(Name.SuggestedActionSubmit)
{
}
