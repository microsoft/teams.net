// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Cards;

/// <summary>
/// Convenience class for creating the data payload to open a dialog
/// from an Action.Submit.
///
/// Abstracts away the <c>msteams: { type: "task/fetch" }</c> protocol detail
/// and sets a reserved <c>dialog_id</c> field for handler routing.
/// </summary>
/// <example>
/// <code>
/// new SubmitAction { Data = new Union&lt;string, SubmitActionData&gt;(new OpenDialogData("simple_form")) }
/// </code>
/// </example>
public class OpenDialogData : SubmitActionData
{
    private const string ReservedKeyword = "dialog_id";

    public OpenDialogData(string dialogId, IDictionary<string, object?>? extraData = null)
    {
        Msteams = new TaskFetchSubmitActionData();
        if (extraData != null)
        {
            foreach (var kvp in extraData)
            {
                NonSchemaProperties[kvp.Key] = kvp.Value;
            }
        }
        NonSchemaProperties[ReservedKeyword] = dialogId;
    }
}