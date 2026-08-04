// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Cards;

/// <summary>
/// Utility class for creating submit data with action-based routing.
///
/// Extends the generated <see cref="SubmitActionData"/> with a convenience constructor
/// that accepts an action identifier for handler routing.
/// </summary>
/// <example>
/// <code>
/// new ExecuteAction { Data = new Union&lt;string, SubmitActionData&gt;(new SubmitData("save_profile", new() { ["entity_id"] = "12345" })) }
/// </code>
/// </example>
public class SubmitData : SubmitActionData
{
    private const string ReservedKeyword = "action";

    public SubmitData(string action, IDictionary<string, object?>? extraData = null)
    {
        if (extraData != null)
        {
            foreach (var kvp in extraData)
            {
                NonSchemaProperties[kvp.Key] = kvp.Value;
            }
        }
        NonSchemaProperties[ReservedKeyword] = action;
    }
}