using System.Text;

using Microsoft.Bot.Builder.Integration.AspNet.Core.Handlers;
using Microsoft.Bot.Core.Schema;
using Microsoft.Bot.Schema;

using Newtonsoft.Json;

namespace Microsoft.Bot.Core.Compat;

internal static class CompatActivity
{
    public static Activity ToCompatActivity(this CoreActivity activity)
    {
        using JsonTextReader reader = new(new StringReader(activity.ToJson()));
        return BotMessageHandlerBase.BotMessageSerializer.Deserialize<Bot.Schema.Activity>(reader)!;
    }

    public static CoreActivity FromCompatActivity(this Activity activity)
    {
        StringBuilder sb = new();
        using StringWriter stringWriter = new(sb);
        using JsonTextWriter json = new(stringWriter);
        BotMessageHandlerBase.BotMessageSerializer.Serialize(json, activity);
        return CoreActivity.FromJsonString(sb.ToString());
    }
}