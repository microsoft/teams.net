using Microsoft.Teams.AI.Annotations;

namespace Samples.Mcp.Prompts;

[Prompt]
public class AnotherPrompt
{
    [Function("test")]
    public string Test()
    {
        return "a test...";
    }
}