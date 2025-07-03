// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.AI.Annotations;

public static partial class Prompt
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
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