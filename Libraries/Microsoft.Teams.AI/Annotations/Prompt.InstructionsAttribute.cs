// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.AI.Templates;

namespace Microsoft.Teams.AI.Annotations;

public static partial class Prompt
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    [Obsolete("Microsoft.Teams.AI is deprecated and will be removed by end of summer 2026.")]
    public class InstructionsAttribute : Attribute
    {
        /// <summary>
        /// the prompts instructions
        /// </summary>
        public ITemplate Instructions { get; private set; }

        public InstructionsAttribute(params string[] Instructions)
        {
            this.Instructions = new StringTemplate(string.Join("\n", Instructions));
        }
    }
}