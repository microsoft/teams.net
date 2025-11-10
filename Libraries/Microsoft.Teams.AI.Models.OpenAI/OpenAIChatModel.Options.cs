// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using OpenAI;

namespace Microsoft.Teams.AI.Models.OpenAI;

public partial class OpenAIChatModel
{
    /// <summary>
    /// the model options
    /// </summary>
    public class Options : OpenAIClientOptions
    {
        /// <summary>
        /// the logger instance
        /// </summary>
        public ILoggerFactory? LoggerFactory { get; set; }
    }
}