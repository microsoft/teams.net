// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Connector;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace Microsoft.Teams.Bot.Compat
{
    /// <summary>
    /// Provides a stub implementation of <see cref="IConnectorClient"/> for compatibility with Bot Framework SDK.
    /// </summary>
    /// <remarks>
    /// This class serves as a minimal adapter to satisfy Bot Framework's requirement for an IConnectorClient instance.
    /// Only the <see cref="Conversations"/> property is implemented; all other members throw <see cref="NotImplementedException"/>.
    /// This design allows legacy bots to access conversation operations through the CompatConversations adapter without
    /// requiring full implementation of unused connector client features.
    /// </remarks>
    /// <param name="conversations">The conversations adapter that handles conversation-related operations.</param>
    internal sealed class CompatConnectorClient(CompatConversations conversations) : IConnectorClient
    {
        /// <summary>
        /// Gets the conversations interface for managing bot conversations.
        /// </summary>
        public IConversations Conversations => conversations;

        public Uri BaseUri
        {
            get => string.IsNullOrWhiteSpace(conversations.ServiceUrl)
                ? throw new InvalidOperationException("ServiceUrl is not set on the compat conversations adapter")
                : new Uri(conversations.ServiceUrl);
            set => conversations.ServiceUrl = value?.ToString();
        }

        public JsonSerializerSettings SerializationSettings => throw new NotImplementedException();

        public JsonSerializerSettings DeserializationSettings => throw new NotImplementedException();

        public ServiceClientCredentials Credentials => new TokenCredentials("compat-stub");

        public IAttachments Attachments => throw new NotImplementedException();


        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            GC.SuppressFinalize(this);
        }
    }
}
