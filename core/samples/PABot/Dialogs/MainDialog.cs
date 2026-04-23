// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Graph.Models;

namespace PABot.Dialogs
{
    /// <summary>
    /// Main dialog that handles the authentication and user interactions.
    /// </summary>
    public class MainDialog : LogoutDialog
    {
        protected readonly ILogger _logger;
        private const string FirstOAuthPrompt = "FirstOAuthPrompt";
        private const string SecondOAuthPrompt = "SecondOAuthPrompt";
        private readonly string _firstConnectionName;
        private readonly string _secondConnectionName;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainDialog"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logger">The logger.</param>
        public MainDialog(IConfiguration configuration, ILogger<MainDialog> logger)
            : base(nameof(MainDialog), configuration["ConnectionName"] ?? "graph")
        {
            _logger = logger;

            // Load connection names from configuration
            _firstConnectionName = configuration["ConnectionName"] ?? "graph";
            _secondConnectionName = configuration["SecondConnectionName"] ?? "graph-2";

            _logger.LogInformation("Using OAuth connections: {FirstConnection} and {SecondConnection}",
                _firstConnectionName, _secondConnectionName);

            // Add first OAuth prompt
            AddDialog(new OAuthPrompt(
                FirstOAuthPrompt,
                new OAuthPromptSettings
                {
                    ConnectionName = _firstConnectionName,
                    Text = $"Please Sign In to the first connection ({_firstConnectionName})",
                    Title = $"Sign In - {_firstConnectionName}",
                    Timeout = 300000, // User has 5 minutes to login (1000 * 60 * 5)
                    EndOnInvalidMessage = true
                }));

            // Add second OAuth prompt
            AddDialog(new OAuthPrompt(
                SecondOAuthPrompt,
                new OAuthPromptSettings
                {
                    ConnectionName = _secondConnectionName,
                    Text = $"Please Sign In to the second connection ({_secondConnectionName})",
                    Title = $"Sign In - {_secondConnectionName}",
                    Timeout = 300000, // User has 5 minutes to login (1000 * 60 * 5)
                    EndOnInvalidMessage = true
                }));

            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                    PromptFirstConnectionAsync,
                    LoginFirstConnectionAsync,
                    PromptSecondConnectionAsync,
                    LoginSecondConnectionAsync,
                    DisplayTokenPhase1Async,
                    DisplayTokenPhase2Async,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        /// <summary>
        /// Prompts the user to sign in to the first connection.
        /// </summary>
        /// <param name="stepContext">The waterfall step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<DialogTurnResult> PromptFirstConnectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("PromptFirstConnectionAsync() called.");
            return await stepContext.BeginDialogAsync(FirstOAuthPrompt, null, cancellationToken);
        }

        /// <summary>
        /// Handles the first connection login step.
        /// </summary>
        /// <param name="stepContext">The waterfall step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<DialogTurnResult> LoginFirstConnectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            TokenResponse firstTokenResponse = (TokenResponse)stepContext.Result;
            if (firstTokenResponse?.Token != null)
            {
                _logger.LogInformation("First connection ({ConnectionName}) authenticated successfully.", _firstConnectionName);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"✓ First connection ({_firstConnectionName}) authenticated successfully!"), cancellationToken);

                // Store the first token in step context values
                stepContext.Values["FirstToken"] = firstTokenResponse;

                // Continue to next step
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                _logger.LogInformation("First connection ({ConnectionName}) authentication failed.", _firstConnectionName);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"First connection ({_firstConnectionName}) login was not successful, please try again."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Prompts the user to sign in to the second connection.
        /// </summary>
        /// <param name="stepContext">The waterfall step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<DialogTurnResult> PromptSecondConnectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("PromptSecondConnectionAsync() called.");
            return await stepContext.BeginDialogAsync(SecondOAuthPrompt, null, cancellationToken);
        }

        /// <summary>
        /// Handles the second connection login step and displays user information.
        /// </summary>
        /// <param name="stepContext">The waterfall step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<DialogTurnResult> LoginSecondConnectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            TokenResponse secondTokenResponse = (TokenResponse)stepContext.Result;
            if (secondTokenResponse?.Token != null)
            {
                _logger.LogInformation("Second connection ({ConnectionName}) authenticated successfully.", _secondConnectionName);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"✓ Second connection ({_secondConnectionName}) authenticated successfully!"), cancellationToken);

                // Store the second token
                stepContext.Values["SecondToken"] = secondTokenResponse;

                // Retrieve the first token
                TokenResponse firstTokenResponse = (TokenResponse)stepContext.Values["FirstToken"];

                try
                {
                    // Use the first token to get user information
                    SimpleGraphClient client = new(firstTokenResponse.Token);
                    User me = await client.GetMeAsync();
                    string title = !string.IsNullOrEmpty(me.JobTitle) ? me.JobTitle : "Unknown";

                    await stepContext.Context.SendActivityAsync($"You're logged in as {me.DisplayName} ({me.UserPrincipalName}); your job title is: {title}");

                    string photo = await client.GetPhotoAsync();

                    if (!string.IsNullOrEmpty(photo))
                    {
                        CardImage cardImage = new(photo);
                        ThumbnailCard card = new(images: new List<CardImage> { cardImage });
                        IMessageActivity reply = MessageFactory.Attachment(card.ToAttachment());

                        await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry! User doesn't have a profile picture to display."), cancellationToken);
                    }

                    return await stepContext.PromptAsync(
                        nameof(ConfirmPrompt),
                        new PromptOptions { Prompt = MessageFactory.Text("Would you like to view your tokens?") },
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing your request.");
                }
            }
            else
            {
                _logger.LogInformation("Second connection ({ConnectionName}) authentication failed.", _secondConnectionName);
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Second connection ({_secondConnectionName}) login was not successful, please try again."), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Displays the tokens if the user confirms.
        /// </summary>
        /// <param name="stepContext">The waterfall step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<DialogTurnResult> DisplayTokenPhase1Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("DisplayTokenPhase1Async() method called.");

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you."), cancellationToken);

            bool result = (bool)stepContext.Result;
            if (result)
            {
                // Pass both tokens to the next step
                return await stepContext.NextAsync(null, cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Displays both tokens to the user.
        /// </summary>
        /// <param name="stepContext">The waterfall step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<DialogTurnResult> DisplayTokenPhase2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("DisplayTokenPhase2Async() method called.");

            // Retrieve both tokens from step context
            TokenResponse firstTokenResponse = (TokenResponse)stepContext.Values["FirstToken"];
            TokenResponse secondTokenResponse = (TokenResponse)stepContext.Values["SecondToken"];

            if (firstTokenResponse != null && secondTokenResponse != null)
            {
                string tokenMessage = $"Here are your tokens:\n\n" +
                                    $"{_firstConnectionName} Connection Token:\n{firstTokenResponse.Token}\n\n" +
                                    $"{_secondConnectionName} Connection Token:\n{secondTokenResponse.Token}";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(tokenMessage), cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Override to handle logout from both OAuth connections.
        /// </summary>
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(
            DialogContext innerDc,
            object options,
            CancellationToken cancellationToken = default)
        {
            DialogTurnResult? result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        /// <summary>
        /// Override to handle logout from both OAuth connections.
        /// </summary>
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(
            DialogContext innerDc,
            CancellationToken cancellationToken = default)
        {
            DialogTurnResult? result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        /// <summary>
        /// Handles logout command by signing out from both OAuth connections.
        /// </summary>
        private async Task<DialogTurnResult?> InterruptAsync(
            DialogContext innerDc,
            CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                string text = innerDc.Context.Activity.Text.ToLowerInvariant();

                // Allow logout anywhere in the command
                if (text.Contains("logout"))
                {
                    // The UserTokenClient encapsulates the authentication processes.
                    UserTokenClient userTokenClient = innerDc.Context.TurnState.Get<UserTokenClient>();

                    // Sign out from both connections
                    await userTokenClient.SignOutUserAsync(
                        innerDc.Context.Activity.From.Id,
                        _firstConnectionName,
                        innerDc.Context.Activity.ChannelId,
                        cancellationToken).ConfigureAwait(false);

                    await userTokenClient.SignOutUserAsync(
                        innerDc.Context.Activity.From.Id,
                        _secondConnectionName,
                        innerDc.Context.Activity.ChannelId,
                        cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("User signed out from both connections: {FirstConnection} and {SecondConnection}",
                        _firstConnectionName, _secondConnectionName);

                    await innerDc.Context.SendActivityAsync(
                        MessageFactory.Text($"You have been signed out from both connections ({_firstConnectionName} and {_secondConnectionName})."),
                        cancellationToken);

                    return await innerDc.CancelAllDialogsAsync(cancellationToken);
                }
            }

            return null;
        }
    }
}
