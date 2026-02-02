// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Rest;
using CompatAssemblyInfo;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

// TODO: Figure out what to do with Agentic Identities. They're all "nulls" here right now.
// The identity is dependent on the incoming payload or supplied in for proactive scenarios.
namespace Microsoft.Teams.Bot.Compat
{
    /// <summary>
    /// Provides a compatibility adapter that bridges the Teams Bot Core <see cref="ConversationClient"/> to the
    /// Bot Framework's <see cref="IConversations"/> interface.
    /// </summary>
    /// <remarks>
    /// This adapter enables legacy Bot Framework bots to use the new Teams Bot Core conversation management
    /// without code changes. It converts between Bot Framework and Core activity formats, handles HTTP operation
    /// responses, and manages custom header translations. All operations delegate to the underlying Core ConversationClient.
    /// </remarks>
    /// <param name="client">The underlying Teams Bot Core ConversationClient that performs the actual conversation operations.</param>
    internal sealed class CompatConversations(ConversationClient client) : IConversations
    {
        internal readonly ConversationClient _client = client;

        /// <summary>
        /// Gets or sets the service URL for the bot service endpoint.
        /// This URL is used for all conversation operations and must be set before making API calls.
        /// </summary>
        internal string? ServiceUrl { get; set; }

        /// <summary>
        /// Creates a new conversation with the specified parameters.
        /// </summary>
        /// <param name="parameters">The conversation parameters including members and activity. Cannot be null.</param>
        /// <param name="customHeaders">Optional custom HTTP headers to include in the request.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an HTTP operation response with
        /// a <see cref="ConversationResourceResponse"/> containing the conversation ID, activity ID, and service URL.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <see cref="ServiceUrl"/> is null or whitespace.</exception>
        public async Task<HttpOperationResponse<ConversationResourceResponse>> CreateConversationWithHttpMessagesAsync(
                Microsoft.Bot.Schema.ConversationParameters parameters,
                Dictionary<string, List<string>>? customHeaders = null,
                CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ServiceUrl);

            Microsoft.Teams.Bot.Core.ConversationParameters convoParams = new()
            {
                Activity = parameters.Activity.FromCompatActivity()
            };
            Dictionary<string, string>? convertedHeaders = ConvertHeaders(customHeaders);

            CreateConversationResponse res = await _client.CreateConversationAsync(
                convoParams,
                new Uri(ServiceUrl),
                AgenticIdentity.FromProperties(convoParams.Activity?.From.Properties),
                convertedHeaders,
                cancellationToken).ConfigureAwait(false);

            ConversationResourceResponse response = new()
            {
                ActivityId = res.ActivityId,
                Id = res.Id,
                ServiceUrl = res.ServiceUrl?.ToString(),
            };

            return new HttpOperationResponse<ConversationResourceResponse>
            {
                Body = response,
                Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            };
        }

        /// <summary>
        /// Deletes an existing activity from a conversation.
        /// </summary>
        /// <param name="conversationId">The unique identifier of the conversation. Cannot be null or whitespace.</param>
        /// <param name="activityId">The unique identifier of the activity to delete. Cannot be null or whitespace.</param>
        /// <param name="customHeaders">Optional custom HTTP headers to include in the request.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an HTTP operation response.</returns>
        /// <exception cref="ArgumentException">Thrown when <see cref="ServiceUrl"/> is null or whitespace.</exception>
        public async Task<HttpOperationResponse> DeleteActivityWithHttpMessagesAsync(string conversationId, string activityId, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ServiceUrl);

            await _client.DeleteActivityAsync(
                conversationId,
                activityId,
                new Uri(ServiceUrl),
                null!,
                ConvertHeaders(customHeaders),
                cancellationToken).ConfigureAwait(false);
            return new HttpOperationResponse
            {
                Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            };
        }

        public async Task<HttpOperationResponse> DeleteConversationMemberWithHttpMessagesAsync(string conversationId, string memberId, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ServiceUrl);

            await _client.DeleteConversationMemberAsync(
                conversationId,
                memberId,
                new Uri(ServiceUrl),
                null!,
                ConvertHeaders(customHeaders),
                cancellationToken).ConfigureAwait(false);
            return new HttpOperationResponse { Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK) };
        }

        public async Task<HttpOperationResponse<IList<ChannelAccount>>> GetActivityMembersWithHttpMessagesAsync(string conversationId, string activityId, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? convertedHeaders = ConvertHeaders(customHeaders);

            IList<Microsoft.Teams.Bot.Core.Schema.ConversationAccount> members = await _client.GetActivityMembersAsync(
                conversationId,
                activityId,
                new Uri(ServiceUrl!),
                null,
                convertedHeaders,
                cancellationToken).ConfigureAwait(false);

            List<ChannelAccount> channelAccounts = [.. members.Select(m => m.ToCompatChannelAccount())];

            return new HttpOperationResponse<IList<ChannelAccount>>
            {
                Body = channelAccounts,
                Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            };
        }

        /// <summary>
        /// Retrieves the list of members participating in a conversation.
        /// </summary>
        /// <param name="conversationId">The unique identifier of the conversation. Cannot be null or whitespace.</param>
        /// <param name="customHeaders">Optional custom HTTP headers to include in the request.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an HTTP operation response with
        /// a list of <see cref="ChannelAccount"/> objects representing the conversation members.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <see cref="ServiceUrl"/> is null or whitespace.</exception>
        public async Task<HttpOperationResponse<IList<ChannelAccount>>> GetConversationMembersWithHttpMessagesAsync(string conversationId, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ServiceUrl);

            Dictionary<string, string>? convertedHeaders = ConvertHeaders(customHeaders);

            IList<Microsoft.Teams.Bot.Core.Schema.ConversationAccount> members = await _client.GetConversationMembersAsync(
                conversationId,
                new Uri(ServiceUrl),
                null,
                convertedHeaders,
                cancellationToken).ConfigureAwait(false);

            List<ChannelAccount> channelAccounts = [.. members.Select(m => m.ToCompatChannelAccount())];

            return new HttpOperationResponse<IList<ChannelAccount>>
            {
                Body = channelAccounts,
                Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            };
        }

        public async Task<HttpOperationResponse<Microsoft.Bot.Schema.PagedMembersResult>> GetConversationPagedMembersWithHttpMessagesAsync(string conversationId, int? pageSize = null, string? continuationToken = null, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ServiceUrl);

            Dictionary<string, string>? convertedHeaders = ConvertHeaders(customHeaders);

            Microsoft.Teams.Bot.Core.PagedMembersResult pagedMembers = await _client.GetConversationPagedMembersAsync(
                conversationId,
                new Uri(ServiceUrl),
                pageSize,
                continuationToken,
                null,
                convertedHeaders,
                cancellationToken).ConfigureAwait(false);

            Microsoft.Bot.Schema.PagedMembersResult result = new()
            {
                ContinuationToken = pagedMembers.ContinuationToken,
                Members = pagedMembers.Members?.Select(m => m.ToCompatChannelAccount()).ToList()
            };

            return new HttpOperationResponse<Microsoft.Bot.Schema.PagedMembersResult>
            {
                Body = result,
                Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            };
        }

        public async Task<HttpOperationResponse<ConversationsResult>> GetConversationsWithHttpMessagesAsync(string? continuationToken = null, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? convertedHeaders = ConvertHeaders(customHeaders);

            GetConversationsResponse conversations = await _client.GetConversationsAsync(
                new Uri(ServiceUrl!),
                continuationToken,
                null,
                convertedHeaders,
                cancellationToken).ConfigureAwait(false);

            ConversationsResult result = new()
            {
                ContinuationToken = conversations.ContinuationToken,
                Conversations = conversations.Conversations?.Select(c => new Microsoft.Bot.Schema.ConversationMembers
                {
                    Id = c.Id,
                    Members = c.Members?.Select(m => m.ToCompatChannelAccount()).ToList()
                }).ToList()
            };

            return new HttpOperationResponse<ConversationsResult>
            {
                Body = result,
                Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            };
        }

        public async Task<HttpOperationResponse<ResourceResponse>> ReplyToActivityWithHttpMessagesAsync(string conversationId, string activityId, Activity activity, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? convertedHeaders = ConvertHeaders(customHeaders);

            CoreActivity coreActivity = activity.FromCompatActivity();

            // ReplyToActivity is not available in ConversationClient, use SendActivityAsync with replyToId in Properties
            coreActivity.Properties["replyToId"] = activityId;
            if (coreActivity.Conversation == null)
            {
                coreActivity.Conversation = new Microsoft.Teams.Bot.Core.Schema.Conversation { Id = conversationId };
            }
            else
            {
                coreActivity.Conversation.Id = conversationId;
            }

            SendActivityResponse response = await _client.SendActivityAsync(coreActivity, convertedHeaders, cancellationToken).ConfigureAwait(false);

            ResourceResponse resourceResponse = new()
            {
                Id = response.Id
            };

            return new HttpOperationResponse<ResourceResponse>
            {
                Body = resourceResponse,
                Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            };
        }

        public async Task<HttpOperationResponse<ResourceResponse>> SendConversationHistoryWithHttpMessagesAsync(string conversationId, Microsoft.Bot.Schema.Transcript transcript, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ServiceUrl);

            Dictionary<string, string>? convertedHeaders = ConvertHeaders(customHeaders);

            Microsoft.Teams.Bot.Core.Transcript coreTranscript = new()
            {
                Activities = transcript.Activities?.Select(a => a.FromCompatActivity() as CoreActivity).ToList()
            };

            SendConversationHistoryResponse response = await _client.SendConversationHistoryAsync(
                conversationId,
                coreTranscript,
                new Uri(ServiceUrl),
                null,
                convertedHeaders,
                cancellationToken).ConfigureAwait(false);

            ResourceResponse resourceResponse = new()
            {
                Id = response.Id
            };

            return new HttpOperationResponse<ResourceResponse>
            {
                Body = resourceResponse,
                Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            };
        }

        /// <summary>
        /// Sends an activity to an existing conversation.
        /// </summary>
        /// <param name="conversationId">The unique identifier of the conversation. Cannot be null or whitespace.</param>
        /// <param name="activity">The activity to send. Cannot be null.</param>
        /// <param name="customHeaders">Optional custom HTTP headers to include in the request.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an HTTP operation response with
        /// a <see cref="ResourceResponse"/> containing the ID of the sent activity.
        /// </returns>
        public async Task<HttpOperationResponse<ResourceResponse>> SendToConversationWithHttpMessagesAsync(string conversationId, Activity activity, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? convertedHeaders = ConvertHeaders(customHeaders);

            CoreActivity coreActivity = activity.FromCompatActivity();

            // Ensure conversation ID is set
            coreActivity.Conversation ??= new Microsoft.Teams.Bot.Core.Schema.Conversation { Id = conversationId };

            SendActivityResponse response = await _client.SendActivityAsync(coreActivity, convertedHeaders, cancellationToken).ConfigureAwait(false);

            ResourceResponse resourceResponse = new()
            {
                Id = response.Id
            };

            return new HttpOperationResponse<ResourceResponse>
            {
                Body = resourceResponse,
                Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            };
        }

        /// <summary>
        /// Updates an existing activity in a conversation.
        /// </summary>
        /// <param name="conversationId">The unique identifier of the conversation. Cannot be null or whitespace.</param>
        /// <param name="activityId">The unique identifier of the activity to update. Cannot be null or whitespace.</param>
        /// <param name="activity">The updated activity content. Cannot be null.</param>
        /// <param name="customHeaders">Optional custom HTTP headers to include in the request.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an HTTP operation response with
        /// a <see cref="ResourceResponse"/> containing the ID of the updated activity.
        /// </returns>
        public async Task<HttpOperationResponse<ResourceResponse>> UpdateActivityWithHttpMessagesAsync(string conversationId, string activityId, Activity activity, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? convertedHeaders = ConvertHeaders(customHeaders);

            CoreActivity coreActivity = activity.FromCompatActivity();

            UpdateActivityResponse response = await _client.UpdateActivityAsync(conversationId, activityId, coreActivity, convertedHeaders, cancellationToken).ConfigureAwait(false);

            ResourceResponse resourceResponse = new()
            {
                Id = response.Id
            };

            return new HttpOperationResponse<ResourceResponse>
            {
                Body = resourceResponse,
                Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            };
        }

        public async Task<HttpOperationResponse<ResourceResponse>> UploadAttachmentWithHttpMessagesAsync(string conversationId, Microsoft.Bot.Schema.AttachmentData attachmentUpload, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ServiceUrl);
            Dictionary<string, string>? convertedHeaders = ConvertHeaders(customHeaders);

            Microsoft.Teams.Bot.Core.AttachmentData coreAttachmentData = new()
            {
                Type = attachmentUpload.Type,
                Name = attachmentUpload.Name,
                OriginalBase64 = attachmentUpload.OriginalBase64,
                ThumbnailBase64 = attachmentUpload.ThumbnailBase64
            };

            UploadAttachmentResponse response = await _client.UploadAttachmentAsync(
                conversationId,
                coreAttachmentData,
                new Uri(ServiceUrl),
                null,
                convertedHeaders,
                cancellationToken).ConfigureAwait(false);

            ResourceResponse resourceResponse = new()
            {
                Id = response.Id
            };

            return new HttpOperationResponse<ResourceResponse>
            {
                Body = resourceResponse,
                Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            };
        }

        private static Dictionary<string, string>? ConvertHeaders(Dictionary<string, List<string>>? customHeaders)
        {
            if (customHeaders == null)
            {
                return null;
            }

            Dictionary<string, string> convertedHeaders = [];
            foreach (KeyValuePair<string, List<string>> kvp in customHeaders)
            {
                convertedHeaders[kvp.Key] = string.Join(",", kvp.Value);
            }

            return convertedHeaders;
        }

        public async Task<HttpOperationResponse<ChannelAccount>> GetConversationMemberWithHttpMessagesAsync(string userId, string conversationId, Dictionary<string, List<string>> customHeaders = null!, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ServiceUrl);

            Dictionary<string, string>? convertedHeaders = ConvertHeaders(customHeaders);

            Microsoft.Teams.Bot.Apps.Schema.TeamsConversationAccount response = await _client.GetConversationMemberAsync<Microsoft.Teams.Bot.Apps.Schema.TeamsConversationAccount>(
                conversationId, userId, new Uri(ServiceUrl), null!, convertedHeaders, cancellationToken).ConfigureAwait(false);

            return new HttpOperationResponse<ChannelAccount>
            {
                Body = response.ToCompatChannelAccount(),
                Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            };

        }
    }
}
