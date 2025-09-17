using System.Text.Json;

namespace Microsoft.Teams.Plugins.External.McpClient;

public class McpToolDetails
{
    /// <summary>
    /// Tool name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Tool description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The input schema for the tool in JSON format
    /// </summary>
    public JsonElement? InputSchema { get; set; }
}
