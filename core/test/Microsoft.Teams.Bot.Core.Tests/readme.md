# Microsoft.Bot.Core.Tests

To run these tests we need to configure the environment variables using a `.runsettings` file, that should be localted in `core/` folder.


```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <TEST_ConversationId>a:17vxw6pGQOb3Zfh8acXT8m_PqHycYpaFgzu2mFMUfkT-h0UskMctq5ZPPc7FIQxn2bx7rBSm5yE_HeUXsCcKZBrv77RgorB3_1_pAdvMhi39ClxQgawzyQ9GBFkdiwOxT</TEST_ConversationId>
      <AzureAd__Instance>https://login.microsoftonline.com/</AzureAd__Instance>
      <AzureAd__ClientId></AzureAd__ClientId>
      <AzureAd__TenantId></AzureAd__TenantId>
      <AzureAd__Scope>https://api.botframework.com/.default</AzureAd__AgentScope>
      <AzureAd__ClientCredentials__0__SourceType>ClientSecret</AzureAd__ClientCredentials__0__SourceType>
      <AzureAd__ClientCredentials__0__ClientSecret></AzureAd__ClientCredentials__0__ClientSecret>
    </EnvironmentVariables>
  </RunConfiguration>
</RunSettings>
```