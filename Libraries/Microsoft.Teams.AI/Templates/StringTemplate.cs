// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


namespace Microsoft.Teams.AI.Templates;

/// <summary>
/// a template that renders a raw string
/// </summary>
[Obsolete("Microsoft.Teams.AI is deprecated and will be removed by end of summer 2026.")]
public class StringTemplate(string? source = null) : ITemplate
{
    public Task<string> Render(object? data = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(source ?? string.Empty);
    }
}