using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Rido.BFLite.Core;

namespace Rido.BFLite.Compat.Adapter;

public class CompatBotAdapter(BotApplication botApplication) : BotAdapter
{
    public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async override Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
    {

        ResourceResponse[] responses = new ResourceResponse[1];
        for (int i = 0; i < activities.Length; i++)
        {
            Core.Schema.Activity a = activities[i].FromCompatActivity();

            string resp = await botApplication.SendActivityAsync(a, cancellationToken);
            responses[i] = new ResourceResponse(id: resp);
        }
        return responses;
    }

    public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


}
