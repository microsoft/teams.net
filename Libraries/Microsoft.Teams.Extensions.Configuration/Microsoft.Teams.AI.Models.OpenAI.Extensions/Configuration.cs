using Microsoft.Extensions.Configuration;

namespace Microsoft.Teams.AI.Models.OpenAI.Extensions;

public static class ConfigurationExtensions
{
    public static OpenAISettings GetOpenAI(this IConfiguration configuration)
    {
        return configuration.GetRequiredSection("OpenAI").Get<OpenAISettings>() ?? throw new Exception("OpenAI Configuration Not Found");
    }
}