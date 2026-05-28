// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.AI.Annotations;

[Obsolete("Microsoft.Teams.AI is deprecated and will be removed by end of summer 2026.")]
public static partial class Prompt
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    [Obsolete("Microsoft.Teams.AI is deprecated and will be removed by end of summer 2026.")]
    public class DescriptionAttribute : Attribute
    {
        /// <summary>
        /// the prompts description
        /// </summary>
        public string Description { get; private set; }

        public DescriptionAttribute(params string[] Description)
        {
            this.Description = string.Join("\n", Description);
        }
    }
}