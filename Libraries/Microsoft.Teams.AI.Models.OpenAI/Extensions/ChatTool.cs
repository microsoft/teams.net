using System.Text.Json;

using Json.Schema;

using OpenAI.Chat;

namespace Microsoft.Teams.AI.Models.OpenAI;

public static partial class MessageExtensions
{
    public static IFunction ToTeams(this ChatTool tool)
    {
        var parameters = tool.FunctionParameters.ToString();

        return new Function(
            tool.FunctionName,
            tool.FunctionDescription,
            JsonSchema.FromText(parameters == string.Empty ? "{}" : parameters),
            () => Task.FromResult<object?>(null)
        );
    }

    public static ChatTool ToOpenAI(this IFunction function)
    {
        return ChatTool.CreateFunctionTool(
            function.Name,
            function.Description,
            function.Parameters is null ? null : BinaryData.FromString(JsonSerializer.Serialize(function.Parameters)),
            false
        );
    }
}