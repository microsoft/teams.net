// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Apps;

namespace AIFileAnalysisBot;

internal sealed class AnalysisRunner(IChatClient chatClient, ILogger<AnalysisRunner> logger)
{
    private const string SystemPrompt = """
        You analyze files supplied by the user.

        Base your answer on the user's message and the attached content. State clearly when the available files do not support
        a conclusion. Do not claim to have inspected files that were not included. Keep the response concise and practical.
        """;

    /// <summary>
    /// Sends one stateless request for the current message and streams the reply.
    ///
    /// SAMPLE GUARDRAIL: nothing is carried between turns. A stateful agent would keep history here, but that would let
    /// a later message silently reuse file content the user did not attach to it, and would resend every image on every
    /// following turn.
    /// </summary>
    public async Task RunAsync(AnalysisRequest request, TeamsStreamingWriter writer, CancellationToken cancellationToken)
    {
        try
        {
            await writer.SendInformativeUpdateAsync("Analyzing files...", cancellationToken);

            ChatMessage[] messages =
            [
                new ChatMessage(ChatRole.System, SystemPrompt),
                new ChatMessage(ChatRole.User, request.Content),
            ];

            await foreach (ChatResponseUpdate update in chatClient.GetStreamingResponseAsync(
                messages, cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    await writer.AppendResponseAsync(update.Text, cancellationToken);
                }
            }

            await writer.FinalizeResponseAsync(new MessageActivityInput().AddAIGenerated(), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "File analysis failed");

            bool rateLimited = (ex as ClientResultException)?.Status == 429
                || ex.Message.StartsWith("429 ", StringComparison.Ordinal);
            MessageActivityInput failure = new MessageActivityInput()
                .WithText(rateLimited
                    ? "The AI service is temporarily rate-limited. Please wait a moment and try again."
                    : "I could not analyze those files. Please try again.")
                .AddAIGenerated();

            await writer.FinalizeResponseAsync(failure, cancellationToken);
        }
    }
}
