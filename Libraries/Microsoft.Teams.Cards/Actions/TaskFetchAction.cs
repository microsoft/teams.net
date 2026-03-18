// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// This class is deprecated. Please use <see cref="TaskFetchSubmitActionData"/> instead. This will be removed in a future version of the SDK.
/// </summary>
[Obsolete("This class is deprecated. Use TaskFetchSubmitActionData instead. This will be removed in a future version of the SDK.")]
public class TaskFetchAction : SubmitAction
{
    public TaskFetchAction(IDictionary<string, object?>? value = null)
    {
        var submitActionData = new SubmitActionData
        {
            Msteams = new TaskFetchSubmitActionData()
        };

        if (value != null)
        {
            foreach (var kvp in value)
            {
                submitActionData.NonSchemaProperties[kvp.Key] = kvp.Value;
            }
        }

        Data = new Union<string, SubmitActionData>(submitActionData);
    }

    public static IDictionary<string, object?> FromObject(object obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        return obj.GetType()
                  .GetProperties()
                  .ToDictionary(
                      p => p.Name,
                      p => (object?)p.GetValue(obj));
    }
}