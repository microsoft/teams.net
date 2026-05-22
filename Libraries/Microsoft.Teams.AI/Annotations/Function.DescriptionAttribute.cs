// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.AI.Annotations;

[Obsolete("Microsoft.Teams.AI is deprecated and will be removed by end of summer 2026.")]
public static partial class Function
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    [Obsolete("Microsoft.Teams.AI is deprecated and will be removed by end of summer 2026.")]
    public class DescriptionAttribute : Attribute
    {
        /// <summary>
        /// the functions description
        /// </summary>
        public string Description { get; private set; }

        public DescriptionAttribute(params string[] Description)
        {
            this.Description = string.Join("\n", Description);
        }
    }
}