// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Logging;

using OpenAI;

namespace Microsoft.Teams.AI.Models.OpenAI;

public partial class OpenAIChatModel
{
    /// <summary>
    /// the model options
    /// </summary>
    [Obsolete("Microsoft.Teams.AI.Models.OpenAI is deprecated and will be removed by end of summer 2026.")]
    public class Options : OpenAIClientOptions
    {
        /// <summary>
        /// the logger instance
        /// </summary>
        public ILogger? Logger { get; set; }
    }
}