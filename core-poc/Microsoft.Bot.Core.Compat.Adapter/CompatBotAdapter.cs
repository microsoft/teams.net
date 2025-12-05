using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Core.Compat.Adapter;

public class CompatBotAdapter(BotApplication botApplication) : BotAdapter
{
    public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
    {

        ResourceResponse[] responses = new ResourceResponse[1];
        for (int i = 0; i < activities.Length; i++)
        {
            Core.Schema.CoreActivity a = activities[i].FromCompatActivity();

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
