using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps;
using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.AI.Prompts;
using Microsoft.Teams.AI.Templates;
using System.Collections.Concurrent;

namespace Samples.AI.Handlers;

/// <summary>
/// Handles feedback loop functionality - sending AI messages with feedback buttons
/// and processing feedback submissions
/// </summary>
public static class FeedbackHandler
{
    /// <summary>
    /// Storage for feedback data. In production, this would be persisted in a database.
    /// </summary>
    public static readonly ConcurrentDictionary<string, FeedbackData> StoredFeedbackByMessageId = new();


    public static ILogger? Logger { get; set; } 

    /// <summary>
    /// Handles the feedback loop command - sends an AI response with feedback buttons
    /// </summary>
    public static async Task HandleFeedbackLoop(OpenAIChatModel model, IContext<Microsoft.Teams.Api.Activities.MessageActivity> context)
    {
        Logger?.LogInformation($"[HANDLER] Feedback loop handler invoked with query: {context.Activity.Text}");

        var prompt = new OpenAIChatPrompt(model, new ChatPromptOptions
        {
            Instructions = new StringTemplate("You are a helpful assistant.")
        });

        Logger?.LogInformation("[HANDLER] Sending query to AI model...");
        var result = await prompt.Send(context.Activity.Text);

        if (result.Content != null)
        {
            Logger?.LogInformation($"[HANDLER] AI response received: {result.Content}");

            // Create message with AI generated indicator and feedback buttons
            var messageActivity = new Microsoft.Teams.Api.Activities.MessageActivity
            {
                Text = result.Content,
            }
            .AddAIGenerated()
            .AddFeedback(); // This adds the thumbs up/down buttons

            Logger?.LogInformation("[HANDLER] Sending message with feedback buttons");
            var sentActivity = await context.Send(messageActivity);

            // Store the feedback data for later retrieval
            if (sentActivity.Id != null)
            {
                StoredFeedbackByMessageId[sentActivity.Id] = new FeedbackData
                {
                    IncomingMessage = context.Activity.Text ?? string.Empty,
                    OutgoingMessage = result.Content,
                    Likes = 0,
                    Dislikes = 0,
                    Feedbacks = new List<string>()
                };
                Logger?.LogInformation($"[HANDLER] Stored feedback data for message ID: {sentActivity.Id}");
            }
        }
        else
        {
            Logger?.LogWarning("[HANDLER] AI did not generate a response");
            await context.Reply("I did not generate a response.");
        }
    }

    /// <summary>
    /// Handles feedback submissions from users
    /// </summary>
    public static void HandleFeedbackSubmission(IContext<Messages.SubmitActionActivity> context)
    {
        Logger?.LogInformation($"[HANDLER] Feedback submission received for activity ID: {context.Activity.Id}");

        if (context.Activity.Value?.ActionValue == null)
        {
            Logger?.LogWarning("[HANDLER] No action value found in feedback submission");
            return;
        }

        Logger?.LogInformation($"[HANDLER] Raw ActionValue: {System.Text.Json.JsonSerializer.Serialize(context.Activity.Value.ActionValue)}");
        // Deserialize ActionValue to a dictionary
        var actionValueJson = System.Text.Json.JsonSerializer.Serialize(context.Activity.Value.ActionValue);
        var actionValue = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(actionValueJson);

        if (actionValue == null)
        {
            Logger?.LogWarning("[HANDLER] Could not parse action value");
            return;
        }

        // Extract reaction (like/dislike) and feedback JSON string
        var reaction = actionValue.ContainsKey("reaction") ? actionValue["reaction"] : null;
        var feedbackJsonString = actionValue.ContainsKey("feedback") ? actionValue["feedback"] : null;

        // Parse the feedback JSON string to extract feedbackText
        string? feedbackText = null;
        if (!string.IsNullOrEmpty(feedbackJsonString))
        {
            try
            {
                var feedbackObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(feedbackJsonString);
                feedbackText = feedbackObj?.ContainsKey("feedbackText") == true ? feedbackObj["feedbackText"] : null;
            }
            catch (System.Text.Json.JsonException ex)
            {
                Logger?.LogWarning($"[HANDLER] Failed to parse feedback JSON: {ex.Message}");
            }
        }

        Logger?.LogInformation($"[HANDLER] Reaction: {reaction}, Feedback Text: {feedbackText}");
        if (context.Activity.ReplyToId == null)
        {
            Logger?.LogWarning($"[HANDLER] No replyToId found for message ID {context.Activity.Id}");
            return;
        }

        // Update stored feedback
        if (StoredFeedbackByMessageId.TryGetValue(context.Activity.ReplyToId, out var existingFeedback))
        {
            if (reaction == "like")
            {
                existingFeedback.Likes++;
                Logger?.LogInformation($"[HANDLER] Incremented likes to {existingFeedback.Likes}");
            }
            else if (reaction == "dislike")
            {
                existingFeedback.Dislikes++;
                Logger?.LogInformation($"[HANDLER] Incremented dislikes to {existingFeedback.Dislikes}");
            }

            if (feedbackText != null)
            {
                existingFeedback.Feedbacks.Add(feedbackText);
                Logger?.LogInformation($"[HANDLER] Added feedback text: '{feedbackText}'. Total feedbacks: {existingFeedback.Feedbacks.Count}");
            }

            // Log feedback summary
            Logger?.LogInformation($"[HANDLER] Feedback summary for message {context.Activity.ReplyToId}: " +
                           $"Likes={existingFeedback.Likes}, Dislikes={existingFeedback.Dislikes}, " +
                           $"Feedbacks={existingFeedback.Feedbacks.Count}");
        }
        else
        {
            Logger?.LogWarning($"[HANDLER] No feedback data found for message ID {context.Activity.ReplyToId}");
        }
    }
}

/// <summary>
/// Data structure for storing feedback information
/// </summary>
public class FeedbackData
{
    public string IncomingMessage { get; set; } = string.Empty;
    public string OutgoingMessage { get; set; } = string.Empty;
    public int Likes { get; set; }
    public int Dislikes { get; set; }
    public List<string> Feedbacks { get; set; } = new();
}
