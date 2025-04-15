namespace Microsoft.Teams.AI;

/// <summary>
/// templates render contextual data
/// into a string
/// </summary>
public interface ITemplate
{
    /// <summary>
    /// render the template
    /// </summary>
    /// <param name="data">the context data</param>
    /// <returns>the rendered string</returns>
    public Task<string> Render(object? data = null, CancellationToken cancellationToken = default);
}