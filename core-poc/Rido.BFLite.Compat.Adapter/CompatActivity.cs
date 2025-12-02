using Microsoft.Bot.Builder.Integration.AspNet.Core.Handlers;
using Newtonsoft.Json;
using System.Text;

namespace Rido.BFLite.Compat.Adapter;

internal static class CompatActivity
{
    public static Microsoft.Bot.Schema.Activity ToCompatActivity(this Rido.BFLite.Core.Schema.Activity activity) =>
        BotMessageHandlerBase.BotMessageSerializer.Deserialize<Microsoft.Bot.Schema.Activity>(new JsonTextReader(new StringReader(activity.ToJson())))!;

    public static Rido.BFLite.Core.Schema.Activity FromCompatActivity(this Microsoft.Bot.Schema.Activity activity)
    {
        StringBuilder sb = new();
        BotMessageHandlerBase.BotMessageSerializer.Serialize(new JsonTextWriter(new StringWriter(sb)), activity);
        return Rido.BFLite.Core.Schema.Activity.FromJsonString(sb.ToString());
    }
}
