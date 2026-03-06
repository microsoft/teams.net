// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a file consent invoke activity.
/// </summary>
public class FileConsentActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileConsentActivity"/> class.
    /// </summary>
    public FileConsentActivity() : base("fileConsent/invoke")
    {
    }
}
