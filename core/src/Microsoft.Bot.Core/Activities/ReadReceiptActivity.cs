// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a read receipt event activity.
/// </summary>
public class ReadReceiptActivity : EventActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadReceiptActivity"/> class.
    /// </summary>
    public ReadReceiptActivity() : base(EventNames.ReadReceipt)
    {
    }
}
