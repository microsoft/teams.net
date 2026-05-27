// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.AI.Annotations;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
[Obsolete("Microsoft.Teams.AI is deprecated and will be removed by end of summer 2026.")]
public class FunctionAttribute : Attribute
{
    /// <summary>
    /// the functions name
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// the functions description
    /// </summary>
    public string? Description { get; private set; }

    public FunctionAttribute(string? Name = null, string? Description = null)
    {
        this.Name = Name;
        this.Description = Description;
    }
}