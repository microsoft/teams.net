using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace Microsoft.Teams.AI.Prompts;

public partial class ChatPrompt<TOptions>
{
    /// <summary>
    /// arguments passed to a chat prompt function
    /// by the model
    /// </summary>
    internal class Args
    {
        [JsonPropertyName("text")]
        [JsonPropertyOrder(0)]
        public required string Text { get; set; }
    }

    /// <summary>
    /// provide another chat prompt to be used
    /// as a function
    /// </summary>
    /// <param name="prompt">the chat prompt</param>
    public ChatPrompt<TOptions> Chain(IChatPrompt<TOptions> prompt)
    {
        Functions.Add(new Function<Args>(
            prompt.Name,
            prompt.Description,
            new JsonSchemaBuilder().FromType<Args>().Build(),
            async (args) =>
            {
                var res = await prompt.Send(args.Text);
                return res.Content;
            }
        ));

        return this;
    }
}