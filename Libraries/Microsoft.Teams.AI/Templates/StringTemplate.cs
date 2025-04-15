
namespace Microsoft.Teams.AI.Templates;

/// <summary>
/// a template that renders a raw string
/// </summary>
public class StringTemplate(string? source = null) : ITemplate
{
    public Task<string> Render(object? data = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(source ?? string.Empty);
    }
}