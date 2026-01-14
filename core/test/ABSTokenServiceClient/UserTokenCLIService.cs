using System.Text.Json;
using Microsoft.Bot.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ABSTokenServiceClient
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
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
            const string userId = "your-user-id";
            const string connectionName = "graph";
            const string channelId = "msteams";

            logger.LogInformation("Application started");

            try
            {
                logger.LogInformation("=== Testing GetTokenStatus ===");
                GetTokenStatusResult[] tokenStatus = await userTokenClient.GetTokenStatusAsync(userId, channelId, null, cancellationToken);
                logger.LogInformation("GetTokenStatus result: {Result}", JsonSerializer.Serialize(tokenStatus, new JsonSerializerOptions { WriteIndented = true }));

                if (tokenStatus[0].HasToken == true)
                {
                    GetTokenResult? tokenResponse = await userTokenClient.GetTokenAsync(userId, connectionName, channelId, null, cancellationToken);
                    logger.LogInformation("GetToken result: {Result}", JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    GetSignInResourceResult req = await userTokenClient.GetSignInResource(userId, connectionName, channelId, null, cancellationToken);
                    logger.LogInformation("GetSignInResource result: {Result}", JsonSerializer.Serialize(req, new JsonSerializerOptions { WriteIndented = true }));

                    Console.WriteLine("Code?");
                    string code = Console.ReadLine()!;

                    GetTokenResult? tokenResponse2 = await userTokenClient.GetTokenAsync(userId, connectionName, channelId, code, cancellationToken);
                    logger.LogInformation("GetToken With Code result: {Result}", JsonSerializer.Serialize(tokenResponse2, new JsonSerializerOptions { WriteIndented = true }));
                }

                Console.WriteLine("Want to signout? y/n");
                string yn = Console.ReadLine()!;
                if ("y".Equals(yn, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await userTokenClient.SignOutUserAsync(userId, connectionName, channelId, cancellationToken);
                        logger.LogInformation("SignOutUser completed successfully");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error during SignOutUser");
                    }
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
