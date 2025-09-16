// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public class TaskFetchAction : SubmitAction
{
    public TaskFetchAction(object value)
    {
        Data = new Union<string, SubmitActionData>(new SubmitActionData
        {
            MsTeams = new TaskFetchSubmitActionData(value)
        });
    }
}