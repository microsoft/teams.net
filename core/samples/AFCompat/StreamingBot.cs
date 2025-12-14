// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using System.Diagnostics;
using System.Text;
using AdaptiveCards.Templating;
using Azure.AI.OpenAI;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Chat;
using Activity = Microsoft.Bot.Schema.Activity;

namespace AFCompat
{
    public class StreamingBot : TeamsActivityHandler
    {
       
        private readonly string? _endpoint;
        private readonly string? _key;
        private readonly string? _deployment;

        private readonly ChatClient _chatClient;
        private readonly string adaptiveCardTemplate = Path.Combine(".", "Resources", "CardTemplate.json");

        public StreamingBot(IConfiguration config)
        {
            _endpoint = "https://ridofoundry.cognitiveservices.azure.com/";
            _key = config["AZURE_OpenAI_KEY"];
            _deployment = "gpt-5-nano";

            ApiKeyCredential credential = new(_key!);
            AzureOpenAIClient azureClient = new(new Uri(_endpoint!), credential);

            _chatClient = azureClient.GetChatClient(_deployment);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            string? userInput = turnContext.Activity.Text?.Trim().ToLower();
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
                string streamId = await BuildAndSendStreamingActivity(turnContext, "Getting the information...", channelData, cancellationToken).ConfigureAwait(false);

                // Send request to chat client with suitable specifications 
                CollectionResult<StreamingChatCompletionUpdate> completionUpdates = _chatClient.CompleteChatStreaming(
                [
                    new SystemChatMessage("You are an AI great at storytelling which creates compelling fantastical stories."),
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
                        await BuildAndSendStreamingActivity(turnContext, contentBuilder.ToString(), channelData, cancellationToken).ConfigureAwait(false);
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
                        await BuildAndSendStreamingActivity(turnContext, contentBuilder.ToString(), channelData, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                await turnContext.SendActivityAsync(ex.Message, cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Builds the activity with the corresponding data for streaming and sends it.
        /// </summary>
        /// <param name="turnContext">Turn context of the bot</param>
        /// <param name="text">Text being streamed and to be sent as part of the activity</param>
        /// <param name="channelData">ChannelData information needed for streaming purposes</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        private async Task<string> BuildAndSendStreamingActivity(
            ITurnContext<IMessageActivity> turnContext,
            string text,
            StreamingChannelData channelData,
            CancellationToken cancellationToken)
        {
            bool isStreamFinal = channelData.StreamType.ToString().Equals(StreamType.Final.ToString());
            Activity streamingActivity = new()
            {
                Type = isStreamFinal ? ActivityTypes.Message : ActivityTypes.Typing,
                Id = channelData.StreamId,
                ChannelData = channelData
            };

            /*
             * For the moment, we need to add the streaming information in 2 places: Entities and ChannelData.
             * to prevent breaking changes in the near future.
             * The final placement for this information will be in Entities once the feature is available to 
             * the public. As per DevPreview timing, data is set in ChannelData.
             */
            var streamingInfoProperties = new
            {
                streamId = channelData.StreamId,
                streamType = channelData.StreamType.ToString(),
                streamSequence = channelData.StreamSequence,
            };

            streamingActivity.Entities =
            [
                new("streaminfo")
                {
                  Properties = JObject.FromObject(streamingInfoProperties)
                }
            ];

            /*
             * We are sending the final streamed message as an Adaptive Card Attachment built 
             * using a template.
             */
            if (isStreamFinal)
            {
                //Build the adaptive card
                AdaptiveCardTemplate template = new(File.ReadAllText(adaptiveCardTemplate));
                var tempData = new
                {
                    finaltStreamText = text
                };
                Attachment attachment = new()
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(template.Expand(tempData)),
                };

                streamingActivity.Attachments = [attachment];
            }
            else if (!string.IsNullOrEmpty(text))
            {
                streamingActivity.Text = text;
            }

            return await SendStreamingActivityAsync(turnContext, streamingActivity, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the activity
        /// </summary>
        /// <param name="turnContext">Turn context of the bot</param>
        /// <param name="streamingActivity">Activity to be sent</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>The messageId</returns>
        /// <exception cref="Exception"></exception>
        private static async Task<string> SendStreamingActivityAsync(ITurnContext<IMessageActivity> turnContext, IActivity streamingActivity, CancellationToken cancellationToken)
        {
            try
            {
                ResourceResponse streamingResponse = await turnContext.SendActivityAsync(streamingActivity, cancellationToken).ConfigureAwait(false);
                return streamingResponse.Id;
            }
            catch (Exception ex)
            {
                ErrorResponseException? errorResponse = ex as ErrorResponseException;
                string excetionTemplate = "Error while sending streaming activity: ";
                await turnContext.SendActivityAsync(MessageFactory.Text(excetionTemplate + errorResponse?.Body?.Error?.Message), cancellationToken).ConfigureAwait(false);
                throw new Exception(excetionTemplate + ex.Message);
            }
        }

        protected override async Task OnInstallationUpdateActivityAsync(ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Conversation.ConversationType == "channel")
            {
                await turnContext.SendActivityAsync(
                    $"Welcome to Streaming demo bot is configured in {turnContext.Activity.Conversation.Name}. Unfurtonately, the streaming feature is not yet available for channels or group chats.", cancellationToken: cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync("Welcome to Streaming demo bot! You can ask me a question and I'll do my best to answer it.", cancellationToken: cancellationToken);

            }
        }
    }
}
