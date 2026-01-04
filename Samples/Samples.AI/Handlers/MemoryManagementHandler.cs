using Microsoft.Teams.AI;
using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.AI.Prompts;
using Microsoft.Teams.AI.Templates;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;

namespace Samples.AI.Handlers;

public static class MemoryManagementHandler
{
    // Simple in-memory store for conversation histories
    // In your application, it may be a good idea to use a more
    // persistent store backed by a database or other storage solution
    private static readonly Dictionary<string, List<IMessage>> ConversationStore = new();

    /// <summary>
    /// Get or create conversation memory for a specific conversation
    /// </summary>
    public static List<IMessage> GetOrCreateConversationMemory(string conversationId)
    {
        if (!ConversationStore.ContainsKey(conversationId))
        {
            Console.WriteLine($"[MEMORY] Creating new conversation memory for conversation: {conversationId}");
            ConversationStore[conversationId] = new List<IMessage>();
        }
        else
        {
            Console.WriteLine($"[MEMORY] Retrieved existing conversation memory for: {conversationId}");
        }

        return ConversationStore[conversationId];
    }

    /// <summary>
    /// Example of stateful conversation handler that maintains conversation history
    /// </summary>
    public static async Task HandleStatefulConversation(OpenAIChatModel model, IContext<MessageActivity> context)
    {
        Console.WriteLine($"[HANDLER] Stateful conversation handler invoked");
        Console.WriteLine($"[HANDLER] User: {context.Activity.From.Name}, Message: '{context.Activity.Text}'");

        // Retrieve existing conversation memory or initialize new one
        var messages = GetOrCreateConversationMemory(context.Activity.Conversation.Id);

        Console.WriteLine($"[HANDLER] Current conversation history: {messages.Count} messages");

        // Create prompt with conversation-specific memory
        var prompt = new OpenAIChatPrompt(model, new ChatPromptOptions
        {
            Instructions = new StringTemplate("You are a helpful assistant that remembers our previous conversation.")
        });

        // Send with existing messages as context
        Console.WriteLine("[HANDLER] Sending message to AI with conversation history...");
        var options = new IChatPrompt<OpenAI.Chat.ChatCompletionOptions>.RequestOptions
        {
            Messages = messages
        };
        var result = await prompt.Send(context.Activity.Text, options);

        if (result.Content != null)
        {
            Console.WriteLine($"[HANDLER] AI response: {result.Content}");

            var message = new MessageActivity
            {
                Text = result.Content,
            }.AddAIGenerated();
            await context.Send(message);

            // Update conversation history
            messages.Add(UserMessage.Text(context.Activity.Text));
            messages.Add(new ModelMessage<string>(result.Content));

            Console.WriteLine($"[HANDLER] Updated conversation history, now has {messages.Count} messages");
        }
        else
        {
            Console.WriteLine("[HANDLER] No content received from AI");
            await context.Reply("I did not generate a response.");
        }
    }

    /// <summary>
    /// Clear memory for a specific conversation
    /// </summary>
    public static Task ClearConversationMemory(string conversationId)
    {
        if (ConversationStore.TryGetValue(conversationId, out var messages))
        {
            var messageCount = messages.Count;
            messages.Clear();
            Console.WriteLine($"[MEMORY] Cleared {messageCount} messages from conversation: {conversationId}");
        }
        else
        {
            Console.WriteLine($"[MEMORY] No conversation history found for: {conversationId}");
        }

        return Task.CompletedTask;
    }
}