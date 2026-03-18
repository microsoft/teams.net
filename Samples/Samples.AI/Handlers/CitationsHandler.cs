using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Apps;

namespace Samples.AI.Handlers;

public static class CitationsHandler
{
    /// <summary>
    /// Demo citations functionality
    /// </summary>
    public static async Task HandleCitationsDemo(IContext<MessageActivity> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[HANDLER] Citations demo handler invoked by user: {context.Activity.From.Name}");

        var citedDocs = new[]
        {
            new { Title = "Weather Documentation", Content = "Weather data shows sunny conditions across the region" },
            new { Title = "Pokemon Database", Content = "Comprehensive database of Pokemon characteristics and abilities" },
            new { Title = "AI Development Guide", Content = "Best practices for integrating AI into Teams applications" }
        };

        var responseText = "Here's some information with citations [1] about weather patterns, " +
                          "[2] Pokemon data, and [3] AI development best practices.";

        Console.WriteLine($"[HANDLER] Creating message with {citedDocs.Length} citations");

        var messageActivity = new MessageActivity
        {
            Text = responseText,
        }.AddAIGenerated();

        // Add citations
        for (int i = 0; i < citedDocs.Length; i++)
        {
            Console.WriteLine($"[HANDLER] Adding citation [{i + 1}]: {citedDocs[i].Title}");
            messageActivity.AddCitation(i + 1, new CitationAppearance
            {
                Name = citedDocs[i].Title,
                Abstract = citedDocs[i].Content
            }
            );
        }

        Console.WriteLine("[HANDLER] Sending message with citations to user");
        await context.Send(messageActivity, cancellationToken);
    }
}