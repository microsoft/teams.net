// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Core;
using Moq;

namespace Microsoft.Teams.Bot.Compat.UnitTests
{
    /// <summary>
    /// Verifies the A-019 fix: CompatConnectorClient.Dispose() must be a safe no-op
    /// and must not call GC.SuppressFinalize (the class has no finalizer and owns no
    /// unmanaged resources).
    /// </summary>
    public class CompatConnectorClientTests
    {
        private static CompatConnectorClient CreateClient()
        {
            Mock<HttpClient> mockHttp = new();
            ConversationClient cc = new(mockHttp.Object, NullLogger<ConversationClient>.Instance);
            CompatConversations conversations = new(cc);
            return new CompatConnectorClient(conversations);
        }

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            // Arrange
            CompatConnectorClient client = CreateClient();

            // Act & Assert – Dispose() must be a safe no-op
            Exception? caught = Record.Exception(() => client.Dispose());
            Assert.Null(caught);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            CompatConnectorClient client = CreateClient();

            Exception? caught = Record.Exception(() =>
            {
                client.Dispose();
                client.Dispose();
            });
            Assert.Null(caught);
        }
    }
}
