using Microsoft.Bot.Builder.Integration.AspNet.Core.Handlers;
using Microsoft.Bot.Core.Schema;

using Newtonsoft.Json;

using System.Text;

namespace Microsoft.Bot.Core.Compat;

internal static class CompatActivity
{
    public static Bot.Schema.Activity ToCompatActivity(this CoreActivity activity)
    {
        using var reader = new JsonTextReader(new StringReader(activity.ToJson()));
        return BotMessageHandlerBase.BotMessageSerializer.Deserialize<Bot.Schema.Activity>(reader)!;
    }

    public static CoreActivity FromCompatActivity(this Bot.Schema.Activity activity)
    {
        StringBuilder sb = new();
        using JsonTextWriter json = new (new StringWriter(sb));
        BotMessageHandlerBase.BotMessageSerializer.Serialize(json, activity);
        return CoreActivity.FromJsonString(sb.ToString());
    }
}
