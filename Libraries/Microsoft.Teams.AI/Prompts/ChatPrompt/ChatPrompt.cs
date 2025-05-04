using System.Reflection;

using Json.Schema;
using Json.Schema.Generation;

using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Models;
using Microsoft.Teams.Common.Extensions;
using Microsoft.Teams.Common.Logging;

namespace Microsoft.Teams.AI.Prompts;

/// <summary>
/// a prompt that can send/receive text
/// messages and expose chat model specific
/// features like streaming/functions
/// </summary>
public interface IChatPrompt : IPrompt
{
    /// <summary>
    /// the message history
    /// </summary>
    public IList<IMessage> Messages { get; }

    /// <summary>
    /// the collection of registered functions
    /// </summary>
    public FunctionCollection Functions { get; }
}

/// <summary>
/// a prompt that can send/receive text
/// messages and expose chat model specific
/// features like streaming/functions
/// </summary>
public interface IChatPrompt<TOptions> : IChatPrompt
{
    /// <summary>
    /// register an error handler
    /// </summary>
    public IChatPrompt<TOptions> OnError(Action<Exception> onError);

    /// <summary>
    /// register an error handler
    /// </summary>
    public IChatPrompt<TOptions> OnError(Func<Exception, Task> onError);

    /// <summary>
    /// send a message via the prompt using string content
    /// </summary>
    /// <param name="text">the message text</param>
    /// <param name="options">the request options</param>
    /// <param name="onChunk">
    /// the stream chunk handler (if notnull streaming is enabled)
    /// </param>
    /// <returns>the models response</returns>
    public Task<ModelMessage<string>> Send(string text, RequestOptions? options = null, OnStreamChunk? onChunk = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// send a message via the prompt using content blocks
    /// </summary>
    /// <param name="content">the message content</param>
    /// <param name="options">the request options</param>
    /// <param name="onChunk">
    /// the stream chunk handler (if notnull streaming is enabled)
    /// </param>
    /// <returns>the models response</returns>
    public Task<ModelMessage<string>> Send(IContent[] content, RequestOptions? options = null, OnStreamChunk? onChunk = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// send a message via the prompt
    /// </summary>
    /// <param name="message">the message to send</param>
    /// <param name="options">the request options</param>
    /// <param name="onChunk">
    /// the stream chunk handler (if notnull streaming is enabled)
    /// </param>
    /// <returns>the models response</returns>
    public Task<ModelMessage<string>> Send(UserMessage<string> message, RequestOptions? options = null, OnStreamChunk? onChunk = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// send a message via the prompt
    /// </summary>
    /// <param name="message">the message to send</param>
    /// <param name="options">the request options</param>
    /// <param name="onChunk">
    /// the stream chunk handler (if notnull streaming is enabled)
    /// </param>
    /// <returns>the models response</returns>
    public Task<ModelMessage<string>> Send(UserMessage<IEnumerable<IContent>> message, RequestOptions? options = null, OnStreamChunk? onChunk = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// options to send when invoking a prompt
    /// </summary>
    public class RequestOptions
    {
        /// <summary>
        /// the conversation history
        /// </summary>
        public IList<IMessage>? Messages { get; set; }

        /// <summary>
        /// the model request options
        /// </summary>
        public TOptions? Request { get; set; }
    }
}

/// <summary>
/// a prompt that can send/receive text
/// messages and expose chat model specific
/// features like streaming/functions
/// </summary>
public partial class ChatPrompt<TOptions> : IChatPrompt<TOptions>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public IList<IMessage> Messages { get; private set; }
    public FunctionCollection Functions { get; private set; }

    protected IChatModel<TOptions> Model { get; }
    protected ITemplate? Template { get; }
    protected ILogger Logger { get; }
    protected IList<IPlugin> Plugins { get; }
    protected IList<IChatPlugin> ChatPlugins => Plugins.Where(p => p is IChatPlugin).Select(p => (IChatPlugin)p).ToList();
    protected event EventHandler<Exception> ErrorEvent;

    public ChatPrompt(IChatModel<TOptions> model, ChatPromptOptions? options = null)
    {
        options ??= new();
        Name = options.Name ?? "Chat";
        Description = options.Description ?? "an agent you can chat with";
        Model = model;
        Template = options.Instructions;
        Messages = options.Messages ?? [];
        Functions = new();
        Logger = (options.Logger ?? new ConsoleLogger()).Child($"AI.{Name}");
        Plugins = [];
        ErrorEvent = (_, ex) => Logger.Error(ex);
    }

    public ChatPrompt(ChatPrompt<TOptions> prompt)
    {
        Name = prompt.Name;
        Description = prompt.Description;
        Messages = prompt.Messages;
        Functions = prompt.Functions;
        Model = prompt.Model;
        Template = prompt.Template;
        Logger = prompt.Logger;
        Plugins = prompt.Plugins;
        ErrorEvent = prompt.ErrorEvent;
    }

    public ChatPrompt(string name, ChatPrompt<TOptions> prompt)
    {
        Name = name;
        Description = prompt.Description;
        Messages = prompt.Messages;
        Functions = prompt.Functions;
        Model = prompt.Model;
        Template = prompt.Template;
        Logger = prompt.Logger.Peer(name);
        Plugins = prompt.Plugins;
        ErrorEvent = prompt.ErrorEvent;
    }

    /// <summary>
    /// create a ChatPrompt from any class
    /// utilizing the ChatPromptAttribute
    /// </summary>
    /// <param name="model">the model to use</param>
    /// <param name="value">the class instance to use</param>
    /// <returns>a ChatPrompt</returns>
    public static ChatPrompt<TOptions> From<T>(IChatModel<TOptions> model, T value, ChatPromptOptions? options = null) where T : class
    {
        var type = value.GetType();
        var promptAttribute = type.GetCustomAttribute<PromptAttribute>();
        var nameAttribute = type.GetCustomAttribute<Prompt.NameAttribute>();
        var descriptionAttribute = type.GetCustomAttribute<Prompt.DescriptionAttribute>();
        var instructionsAttribute = type.GetCustomAttribute<Prompt.InstructionsAttribute>();

        if (promptAttribute is null)
        {
            throw new Exception("only types utilizing the ChatPromptAttribute can be turned into a ChatPrompt");
        }

        var name = promptAttribute.Name ?? nameAttribute?.Name ?? type.Name;
        var description = promptAttribute.Description ?? descriptionAttribute?.Description;
        var instructions = promptAttribute.Instructions ?? instructionsAttribute?.Instructions;
        options ??= new ChatPromptOptions().WithName(name);

        if (description is not null)
        {
            options = options.WithDescription(description);
        }

        if (instructions is not null)
        {
            options = options.WithInstructions(instructions);
        }

        var prompt = new ChatPrompt<TOptions>(model, options);

        foreach (var method in type.GetMethods())
        {
            var functionAttribute = method.GetCustomAttribute<FunctionAttribute>();
            var functionDescriptionAttribute = method.GetCustomAttribute<Annotations.Function.DescriptionAttribute>();

            if (functionAttribute is null) continue;

            var parameters = method.GetParameters().Select(p =>
            {
                var name = p.GetCustomAttribute<ParamAttribute>()?.Name ?? p.Name ?? p.Position.ToString();
                var schema = new JsonSchemaBuilder().FromType(p.ParameterType).Build();
                var required = !p.IsOptional;
                return (name, schema, required);
            });

            var schema = new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(parameters.Select(item => (item.name, item.schema)).ToArray())
                .Required(parameters.Where(item => item.required).Select(item => item.name));

            var function = new Function(
                functionAttribute.Name ?? method.Name,
                functionAttribute.Description ?? functionDescriptionAttribute?.Description,
                parameters.Count() > 0 ? schema.Build() : null,
                method.CreateDelegate(value)
            );

            prompt.Function(function);
        }

        return prompt;
    }
}