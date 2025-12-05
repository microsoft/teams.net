using Microsoft.Bot.Builder.Integration.AspNet.Core.Handlers;

using Newtonsoft.Json;

using System.Text;

namespace Microsoft.Bot.Core.Compat.Adapter;

internal static class CompatActivity
{
    public static Bot.Schema.Activity ToCompatActivity(this Schema.CoreActivity activity) =>
        BotMessageHandlerBase.BotMessageSerializer.Deserialize<Bot.Schema.Activity>(new JsonTextReader(new StringReader(activity.ToJson())))!;

    public static Schema.CoreActivity FromCompatActivity(this Bot.Schema.Activity activity)
    {
        StringBuilder sb = new();
        BotMessageHandlerBase.BotMessageSerializer.Serialize(new JsonTextWriter(new StringWriter(sb)), activity);
        return Microsoft.Bot.Core.Schema.CoreActivity.FromJsonString(sb.ToString());
    }
}
