using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Bot.Core;
using System.Text.Json;

namespace ABSTokenServiceClient
{
    internal class UserTokenCLIService(UserTokenClient userTokenClient, ILogger<UserTokenCLIService> logger) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return ExecuteAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            //const string userId = "29:10n4Hk6RsMPuLvAxMNd2zEYU2w1dpvsiLC4QcffJ84rCMp_TKJO_dMzosR4d_K67eAumKyxTzXVYqHQWzRf2ukg";
            const string userId = "29:1z8ZE78C9fd__EBOAY7Xd9QWs_9QMRVpuJK8ad47JE1hTXiiHiTQQxVbreKRFGM1Bc7gqkroHiQdEeSflOyUB4A";
            const string connectionName = "graph";
            const string channelId = "msteams";

            logger.LogInformation("Application started");

            try
            {
                string token;
                logger.LogInformation("=== Testing GetTokenStatus ===");
                GetTokenStatusResult[] tokenStatus = await userTokenClient.GetTokenStatusAsync(userId, channelId, null, cancellationToken);
                logger.LogInformation("GetTokenStatus result: {Result}", JsonSerializer.Serialize(tokenStatus, new JsonSerializerOptions { WriteIndented = true }));

                if (tokenStatus[0].HasToken == true)
                {
                    GetTokenResult tokenResponse = await userTokenClient.GetTokenAsync(userId, connectionName, channelId, null, cancellationToken);
                    token = tokenResponse.Token!;
                    logger.LogInformation("GetToken result: {Result}", JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    GetTokenOrSignInResourceResult req = await userTokenClient.GetTokenOrSignInResource(userId, connectionName, channelId, null, cancellationToken);
                    logger.LogInformation("GetSignInResource result: {Result}", JsonSerializer.Serialize(req, new JsonSerializerOptions { WriteIndented = true }));

                    Console.WriteLine("Code?");
                    string code = Console.ReadLine()!;

                    GetTokenResult tokenResponse2 = await userTokenClient.GetTokenAsync(userId, connectionName, channelId, code, cancellationToken);
                    token = tokenResponse2.Token!;
                    logger.LogInformation("GetToken With Code result: {Result}", JsonSerializer.Serialize(tokenResponse2, new JsonSerializerOptions { WriteIndented = true }));
                }

                Console.WriteLine("Want to signout? y/n");
                string yn = Console.ReadLine()!;
                if ("y".Equals(yn, StringComparison.OrdinalIgnoreCase))
                {
                    await userTokenClient.SignOutUserAsync(userId, connectionName, channelId, cancellationToken);
                    logger.LogInformation("SignOutUser completed successfully");
                }
            }
            catch (Exception ex)
            {

                logger.LogError(ex, "Error during API testing");
            }

            logger.LogInformation("Application completed successfully");
        }
    }
}
