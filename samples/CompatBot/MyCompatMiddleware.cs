// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;

namespace CompatBot
{
    public class MyCompatMiddleware : Microsoft.Bot.Builder.IMiddleware
    {
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("MyCompatMiddleware: OnTurnAsync");
            Console.WriteLine(turnContext.Activity.Text);

            await turnContext.SendActivityAsync(MessageFactory.Text("Hello from MyCompatMiddleware!"), cancellationToken);

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}
