# Tests

.vscode/settings.json

```json
{
  "dotnet.unitTests.runSettingsPath": "./.runsettings"
}
```


.runsettings
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <MY_VAR>test_value</MY_VAR>
      <TEST_CONVERSATIONID>19:9f2af1bee7cc4a71af25ac72478fd5c6@thread.tacv2</TEST_CONVERSATIONID>
      <AzureAd__Instance>https://login.microsoftonline.com/</AzureAd__Instance>
      <AzureAd__ClientId></AzureAd__ClientId>
      <AzureAd__TenantId></AzureAd__TenantId>
      <AzureAd__ClientCredentials__0__SourceType>ClientSecret</AzureAd__ClientCredentials__0__SourceType>
      <AzureAd__ClientCredentials__0__ClientSecret></AzureAd__ClientCredentials__0__ClientSecret>
      <Logging__LogLevel__Default>Warning</Logging__LogLevel__Default>
      <Logging__LogLevel__Microsoft.Bot>Information</Logging__LogLevel__Microsoft.Bot>
    </EnvironmentVariables>
  </RunConfiguration>
</RunSettings>
```