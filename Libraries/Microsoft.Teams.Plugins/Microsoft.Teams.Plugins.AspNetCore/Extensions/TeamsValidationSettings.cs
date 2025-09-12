namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public class TeamsValidationSettings
{
    public string OpenIdMetadataUrl = "https://login.botframework.com/v1/.well-known/openidconfiguration";
    public List<string> Audiences = [];
    public List<string> Issuers = [
        "https://api.botframework.com",
            "https://sts.windows.net/d6d49420-f39b-4df7-a1dc-d59a935871db/",
            "https://login.microsoftonline.com/d6d49420-f39b-4df7-a1dc-d59a935871db/v2.0",
            "https://sts.windows.net/f8cdef31-a31e-4b4a-93e4-5f571e91255a/",
            "https://login.microsoftonline.com/f8cdef31-a31e-4b4a-93e4-5f571e91255a/v2.0",
            "https://sts.windows.net/69e9b82d-4842-4902-8d1e-abc5b98a55e8/",
            "https://login.microsoftonline.com/69e9b82d-4842-4902-8d1e-abc5b98a55e8/v2.0",
        ];

    public void AddDefaultAudiences(string ClientId)
    {
        if (ClientId is not null && !Audiences.Contains(ClientId))
            Audiences.Add(ClientId);

        var apiAudience = $"api://{ClientId}";
        if (ClientId is not null && !Audiences.Contains(apiAudience))
            Audiences.Add(apiAudience);
    }
}
