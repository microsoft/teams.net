# Teams SDK Integration Tests

This project runs integration tests against Teams Server (SMBA/APX) using bot and agentic identitities.

## RunSettings

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
	<RunConfiguration>
		<EnvironmentVariables>
			<!-- Azure AD App Registration -->
			<AzureAd__Instance>https://login.microsoftonline.com/</AzureAd__Instance>
			<AzureAd__TenantId>/AzureAd__TenantId>
			<AzureAd__ClientId>/AzureAd__ClientId>
			<AzureAd__ClientSecret>/AzureAd__ClientSecret>
			<AzureAd__ClientCredentials__0__SourceType>ClientSecret</AzureAd__ClientCredentials__0__SourceType>
			<AzureAd__ClientCredentials__0__ClientSecret>/AzureAd__ClientCredentials__0__ClientSecret>

			<!-- Teams Service URL -->
			<TEST_SERVICEURL></TEST_SERVICEURL>
			<!--https://pilot1.botapi.skype.com/amer https://smba.trafficmanager.net/teams/-->
			<!-- Core test identifiers -->
			
			<TEST_CONVERSATIONID></TEST_CONVERSATIONID>

			<TEST_USER_ID></TEST_USER_ID>
			<TEST_TEAMID></TEST_TEAMID>
			<TEST_CHANNELID></TEST_CHANNELID>
			<TEST_MEETINGID></TEST_MEETINGID>
			<TEST_TENANTID></TEST_TENANTID>

			<!-- Agentic identity (optional) -->
			<TEST_AGENTIC_APPID></TEST_AGENTIC_APPID>
			<TEST_AGENTIC_USERID></TEST_AGENTIC_USERID>

			<!-- Optional -->
			<TEST_USER_ID_2></TEST_USER_ID_2>
			<TEST_CONNECTION_NAME></TEST_CONNECTION_NAME>
			<TEST_OPERATION_ID></TEST_OPERATION_ID>
		</EnvironmentVariables>
	</RunConfiguration>
</RunSettings>

```

## Test Runs

to run the tests, use the following command in the terminal:

```bash
dotnet test --logger "trx;LogFileName=botid-prod.trx" -s .\IntegrationTests\botid-prod.runsettings --results-directory "C:\_code\core-teams.net\core\TestResults"

dotnet test --logger "trx;LogFileName=botid-canary.trx" -s .\IntegrationTests\botid-canary.runsettings --results-directory "C:\_code\core-teams.net\core\TestResults"

dotnet test --logger "trx;LogFileName=agenticid-prod.trx" -s .\IntegrationTests\agenticid-prod.runsettings --results-directory "C:\_code\core-teams.net\core\TestResults"

dotnet test --logger "trx;LogFileName=agenticid-canary.trx" -s .\IntegrationTests\agenticid-canary.runsettings --results-directory "C:\_code\core-teams.net\core\TestResults"

```
