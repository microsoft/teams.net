// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AdaptiveCards.Templating;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Schema;
using OpenAI;
using OpenAI.Chat;

namespace AFBot;

public class AFBotApplication : BotApplication
{
    private readonly string? _endpoint;
    private readonly string? _key;
    private readonly string? _deployment;

    private readonly ChatClient _chatClient;

    public AFBotApplication(
        ConversationClient conversationClient,
        IConfiguration config,
        ILogger<BotApplication> logger,
        string serviceKey = "AzureAd") : base(conversationClient, config, logger, serviceKey)
    {

        _endpoint = "https://ridofoundry.cognitiveservices.azure.com/";
        _key = config["AZURE_OpenAI_KEY"];
        _deployment = "gpt-5-nano";

        ApiKeyCredential credential = new(_key!);
        AzureOpenAIClient azureClient = new(new Uri(_endpoint!), credential);

        _chatClient = azureClient.GetChatClient(_deployment);

        OnActivity = async (activity, cancellationToken) =>
        {
            string? userInput = activity.Text?.Trim().ToLower();
            try
            {
                StringBuilder contentBuilder = new();
                Stopwatch stopwatch = new();
                int streamSequence = 1; // Sream sequence should always start with 1. 
                int rps = 1000; // The current allowance is 1 RPS.

                /*
                 * We can send an initial streaming message as informative while we get the response from the LLM setting the StreamType to Informative.
                 * This action is helpful to get the streaming sequence started and get messageId back, which we will use later as the StreamId
                 */
                StreamingChannelData channelData = new()
                {
                    StreamType = StreamType.Informative,
                    StreamSequence = streamSequence,
                };
                string streamId = await BuildAndSendStreamingActivity(activity, "Getting the information...", channelData, cancellationToken).ConfigureAwait(false);

                // Send request to chat client with suitable specifications 
                CollectionResult<StreamingChatCompletionUpdate> completionUpdates = _chatClient.CompleteChatStreaming(
                [
                    new SystemChatMessage("You are an expert acronym maker, made an acronym made up from the first three characters of the user's message. " +
                                            "Some examples: OMW on my way, BTW by the way, TVM thanks very much, and so on." +
                                            "Always respond with the three complete words only, and include a related emoji at the end."),
                    new UserChatMessage (userInput),
                ],
                cancellationToken: cancellationToken);

                stopwatch.Start(); // Starting stopwatch to chunk by RPS (elapsedMiliseconds)

                foreach (StreamingChatCompletionUpdate streamingChatUpdate in completionUpdates)
                {
                    streamSequence++; // Increment the streamSequence number per each update received for internal purposes

                    /*
                     * If the streaming has ended for some reason, build the final message seeting the ChannelSata.StreamType to Final.
                     * Send the message to the bot and break/continue to prevent further processing.
                     */
                    if (streamingChatUpdate.FinishReason != null)
                    {
                        channelData = new StreamingChannelData
                        {
                            StreamType = StreamType.Final,
                            StreamSequence = streamSequence,
                            StreamId = streamId
                        };
                        await BuildAndSendStreamingActivity(activity,contentBuilder.ToString(), channelData, cancellationToken).ConfigureAwait(false);
                        break;
                    }

                    /*
                     * Teams Content Streaming feature needs bot developers to build chunks from the LLM responses. 
                     * So, we accumulate what is being send and once RPS is reached request is sent.
                     */

                    foreach (ChatMessageContentPart contentPart in streamingChatUpdate.ContentUpdate)
                    {
                        contentBuilder.Append(contentPart.Text);
                    }

                    if (contentBuilder.Length > 0 && stopwatch.ElapsedMilliseconds > rps)
                    {
                        channelData = new StreamingChannelData
                        {
                            StreamType = StreamType.Streaming,
                            StreamSequence = streamSequence,
                            StreamId = streamId
                        };

                        stopwatch.Restart(); // Restart the stopwatch for the next chunk
                        await BuildAndSendStreamingActivity(activity,contentBuilder.ToString(), channelData, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                // await turnContext.SendActivityAsync(ex.Message, cancellationToken: cancellationToken);
                throw;
            }
        };
    }

    private async Task<string> BuildAndSendStreamingActivity(
            CoreActivity activity,
            string text,
            StreamingChannelData channelData,
            CancellationToken cancellationToken)
    {
        //ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(channelData);
        bool isStreamFinal = channelData?.StreamType?.ToString().Equals(StreamType.Final.ToString()) ?? false;
        CoreActivity streamingActivity = new()
        {
            Type = isStreamFinal ? ActivityTypes.Message : ActivityTypes.Typing,
            Id = channelData?.StreamId!,
            ChannelData = channelData,
            Conversation = activity?.Conversation!,
            From = activity?.Recipient!,
            Recipient = activity?.From!,
            ServiceUrl = activity?.ServiceUrl!,
        };

        /*
         * For the moment, we need to add the streaming information in 2 places: Entities and ChannelData.
         * to prevent breaking changes in the near future.
         * The final placement for this information will be in Entities once the feature is available to 
         * the public. As per DevPreview timing, data is set in ChannelData.
         */
        var streamingInfoProperties = new
        {
            streamId = channelData?.StreamId,
            streamType = channelData?.StreamType?.ToString(),
            streamSequence = channelData?.StreamSequence,
        };

        streamingActivity.Entities =[];
        streamingActivity.Entities.Add(new JsonObject
        {
            ["type"] = "streamingInfo",
            ["streamId"] = streamingInfoProperties.streamId,
            ["streamType"] = streamingInfoProperties.streamType,
            ["streamSequence"] = streamingInfoProperties.streamSequence,
        });

        /*
         * We are sending the final streamed message as an Adaptive Card Attachment built 
         * using a template.
         */
        if (isStreamFinal)
        {
                string adaptiveCardTemplate = Path.Combine(".", "CardTemplate.json");
            //Build the adaptive card
            AdaptiveCardTemplate template = new(File.ReadAllText(adaptiveCardTemplate));
            var tempData = new
            {
                finaltStreamText = text
            };
            //Attachment attachment = new()
            //{
            //    ContentType = "application/vnd.microsoft.card.adaptive",
            //    Content = JsonConvert.DeserializeObject(template.Expand(tempData)),
            //};

            JsonNode? attachment = JsonNode.Parse(template.Expand(tempData));
            streamingActivity.Properties.Add("attachments", attachment);
            //streamingActivity.Attachments = [attachment];
        }
        else if (!string.IsNullOrEmpty(text))
        {
            streamingActivity.Text = text;
        }

        return await SendStreamingActivityAsync(streamingActivity, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> SendStreamingActivityAsync(CoreActivity streamingActivity, CancellationToken cancellationToken)
    {
        try
        {
            string streamingResponse = await base.SendActivityAsync(streamingActivity, cancellationToken).ConfigureAwait(false);

            return streamingResponse;
        }
        catch (Exception ex)
        {
            string excetionTemplate = "Error while sending streaming activity: ";
            //await base.SendActivityAsync(MessageFactory.Text(excetionTemplate + errorResponse?.Body?.Error?.Message), cancellationToken).ConfigureAwait(false);
            throw new Exception(excetionTemplate + ex.Message);
        }
    }
}
