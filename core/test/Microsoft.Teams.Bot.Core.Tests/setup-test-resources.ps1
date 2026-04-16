#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Sets up Microsoft Teams integration test resources using Microsoft Graph API.

.DESCRIPTION
    Authenticates with client credentials, discovers or creates Teams resources
    (team, channel, members, meeting), and outputs values for integration.runsettings.

.NOTES
    Required Graph API application permissions on the app registration:
      - Team.ReadBasic.All          (list teams)
      - Channel.ReadBasic.All       (list channels)
      - TeamMember.Read.All         (list team members)
      - OnlineMeetings.ReadWrite.All (create meetings)
      - User.Read.All               (resolve user details)

    Grant admin consent for these permissions in Azure Portal:
      Azure AD > App registrations > [your app] > API permissions > Grant admin consent

.EXAMPLE
    pwsh core/test/Microsoft.Teams.Bot.Core.Tests/setup-test-resources.ps1
#>

param(
    [string]$EnvFile = "$PSScriptRoot/../../e2e-test-bot.env"
)

$ErrorActionPreference = "Stop"

# --- Load credentials from env file ---
if (-not (Test-Path $EnvFile)) {
    Write-Error "Env file not found: $EnvFile"
    exit 1
}

$envVars = @{}
Get-Content $EnvFile | ForEach-Object {
    if ($_ -match '^\s*([^#=]+?)\s*=\s*(.+?)\s*$') {
        $envVars[$Matches[1]] = $Matches[2]
    }
}

$clientId     = $envVars["CLIENT_ID"]
$clientSecret = $envVars["CLIENT_SECRET"]
$tenantId     = $envVars["TENANT_ID"]

if (-not $clientId -or -not $clientSecret -or -not $tenantId) {
    Write-Error "Missing CLIENT_ID, CLIENT_SECRET, or TENANT_ID in $EnvFile"
    exit 1
}

Write-Host "`n=== Teams Integration Test Setup ===" -ForegroundColor Cyan
Write-Host "Tenant:    $tenantId"
Write-Host "Client ID: $clientId`n"

# --- Acquire Graph token ---
Write-Host "Acquiring Graph API token..." -ForegroundColor Yellow

$tokenBody = @{
    client_id     = $clientId
    client_secret = $clientSecret
    scope         = "https://graph.microsoft.com/.default"
    grant_type    = "client_credentials"
}

try {
    $tokenResponse = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" `
        -Method Post -Body $tokenBody -ContentType "application/x-www-form-urlencoded"
    $token = $tokenResponse.access_token
    Write-Host "Token acquired.`n" -ForegroundColor Green
} catch {
    Write-Error "Failed to acquire token: $($_.Exception.Message)"
    exit 1
}

$headers = @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" }

function Invoke-Graph {
    param([string]$Uri, [string]$Method = "GET", $Body = $null)
    $params = @{ Uri = $Uri; Method = $Method; Headers = $headers }
    if ($Body) { $params.Body = ($Body | ConvertTo-Json -Depth 10) }
    try {
        return Invoke-RestMethod @params
    } catch {
        $status = $_.Exception.Response.StatusCode.value__
        $detail = $_.ErrorDetails.Message | ConvertFrom-Json -ErrorAction SilentlyContinue
        $msg = if ($detail.error.message) { $detail.error.message } else { $_.Exception.Message }
        Write-Warning "Graph $Method $Uri -> $status : $msg"
        return $null
    }
}

# --- Step 1: List teams ---
Write-Host "Fetching teams..." -ForegroundColor Yellow
$teamsResponse = Invoke-Graph "https://graph.microsoft.com/v1.0/groups?`$filter=resourceProvisioningOptions/Any(x:x eq 'Team')&`$select=id,displayName&`$top=25"

if (-not $teamsResponse -or -not $teamsResponse.value -or $teamsResponse.value.Count -eq 0) {
    Write-Error "No teams found. Ensure the app has Team.ReadBasic.All permission with admin consent."
    exit 1
}

$teams = $teamsResponse.value
Write-Host "`nAvailable teams:" -ForegroundColor Cyan
for ($i = 0; $i -lt $teams.Count; $i++) {
    Write-Host "  [$i] $($teams[$i].displayName) ($($teams[$i].id))"
}

$teamIndex = Read-Host "`nSelect a team [0-$($teams.Count - 1)]"
$selectedTeam = $teams[[int]$teamIndex]
$teamGroupId = $selectedTeam.id
Write-Host "Selected: $($selectedTeam.displayName)`n" -ForegroundColor Green

# --- Step 2: Get team internal ID (19:xxx format) ---
$teamDetails = Invoke-Graph "https://graph.microsoft.com/v1.0/teams/$teamGroupId"
$teamInternalId = $teamDetails.internalId
if (-not $teamInternalId) {
    # Fall back to using the general channel thread ID
    Write-Warning "Could not get team internalId, will derive from General channel."
}

# --- Step 3: List channels ---
Write-Host "Fetching channels..." -ForegroundColor Yellow
$channelsResponse = Invoke-Graph "https://graph.microsoft.com/v1.0/teams/$teamGroupId/channels?`$select=id,displayName,membershipType"

if (-not $channelsResponse -or -not $channelsResponse.value) {
    Write-Error "No channels found."
    exit 1
}

$channels = $channelsResponse.value
Write-Host "`nAvailable channels:" -ForegroundColor Cyan
for ($i = 0; $i -lt $channels.Count; $i++) {
    $ch = $channels[$i]
    $tag = if ($ch.membershipType -eq "standard" -and $ch.displayName -eq "General") { " (General)" } else { "" }
    Write-Host "  [$i] $($ch.displayName)$tag ($($ch.id))"
}

$channelIndex = Read-Host "`nSelect a channel for tests [0-$($channels.Count - 1)]"
$selectedChannel = $channels[[int]$channelIndex]
Write-Host "Selected: $($selectedChannel.displayName)`n" -ForegroundColor Green

# The General channel ID is also usable as a conversation ID for channel-scoped messages
$generalChannel = $channels | Where-Object { $_.displayName -eq "General" } | Select-Object -First 1

# Use team internalId if available, otherwise derive from channel ID pattern
if (-not $teamInternalId -and $generalChannel) {
    $teamInternalId = $generalChannel.id
}

# --- Step 4: List team members ---
Write-Host "Fetching team members..." -ForegroundColor Yellow
$membersResponse = Invoke-Graph "https://graph.microsoft.com/v1.0/teams/$teamGroupId/members?`$top=25"

if (-not $membersResponse -or -not $membersResponse.value) {
    Write-Error "No members found. Ensure the app has TeamMember.Read.All permission."
    exit 1
}

$members = $membersResponse.value
Write-Host "`nTeam members:" -ForegroundColor Cyan
for ($i = 0; $i -lt $members.Count; $i++) {
    $m = $members[$i]
    $roles = if ($m.roles -and $m.roles.Count -gt 0) { " [$($m.roles -join ', ')]" } else { "" }
    Write-Host "  [$i] $($m.displayName)$roles (AAD: $($m.userId))"
}

$userIndex = Read-Host "`nSelect the primary test user [0-$($members.Count - 1)]"
$selectedUser = $members[[int]$userIndex]
$aadUserId = $selectedUser.userId
Write-Host "Selected: $($selectedUser.displayName)`n" -ForegroundColor Green

# Teams Bot Framework MRI format for AAD users
$userMri = "29:$aadUserId"

# Optional: second user for group conversation tests
$userMri2 = ""
if ($members.Count -gt 1) {
    $pickSecond = Read-Host "Select a second test user (optional, press Enter to skip) [0-$($members.Count - 1)]"
    if ($pickSecond -ne "") {
        $secondUser = $members[[int]$pickSecond]
        $userMri2 = "29:$($secondUser.userId)"
        Write-Host "Second user: $($secondUser.displayName)`n" -ForegroundColor Green
    }
}

# --- Step 5: Create an online meeting ---
Write-Host "Creating a test meeting..." -ForegroundColor Yellow

# App-only meetings require OnlineMeetings.ReadWrite.All and a user to act on behalf of
$meetingBody = @{
    startDateTime = (Get-Date).AddHours(1).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    endDateTime   = (Get-Date).AddHours(2).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    subject       = "SDK Integration Test Meeting"
}

$meetingResponse = Invoke-Graph "https://graph.microsoft.com/v1.0/users/$aadUserId/onlineMeetings" -Method POST -Body $meetingBody

$meetingId = ""
if ($meetingResponse) {
    # The joinMeetingIdSettings.joinMeetingId or the meeting id itself
    $meetingId = $meetingResponse.id
    Write-Host "Meeting created: $($meetingResponse.subject) (ID: $meetingId)`n" -ForegroundColor Green
} else {
    Write-Warning "Could not create meeting. You may need OnlineMeetings.ReadWrite.All permission."
    Write-Host "You can set TEST_MEETINGID manually later.`n"
}

# --- Step 6: Determine conversation ID ---
# For channel-scoped tests, the channel ID works as conversation ID.
# For 1:1 bot conversations, the bot must be installed and must have received a conversationUpdate.
# We'll use the selected channel as the default conversation ID.
$conversationId = $selectedChannel.id

Write-Host "`n=== Results ===" -ForegroundColor Cyan
Write-Host "Copy these values into integration.runsettings:`n"

$results = [ordered]@{
    "AzureAd__TenantId"    = $tenantId
    "AzureAd__ClientId"    = $clientId
    "AzureAd__ClientSecret"= $clientSecret
    "TEST_SERVICEURL"      = "https://smba.trafficmanager.net/teams/"
    "TEST_CONVERSATIONID"  = $conversationId
    "TEST_USER_ID"         = $userMri
    "TEST_TEAMID"          = if ($teamInternalId) { $teamInternalId } else { $teamGroupId }
    "TEST_CHANNELID"       = $selectedChannel.id
    "TEST_MEETINGID"       = $meetingId
    "TEST_TENANTID"        = $tenantId
}

if ($userMri2) { $results["TEST_USER_ID_2"] = $userMri2 }

foreach ($kv in $results.GetEnumerator()) {
    $display = if ($kv.Key -like "*Secret*") { "********" } else { $kv.Value }
    Write-Host "  $($kv.Key) = $display"
}

# --- Step 7: Write to runsettings ---
$runSettingsPath = "$PSScriptRoot/integration.runsettings"
$updateRunsettings = Read-Host "`nUpdate integration.runsettings automatically? [y/N]"

if ($updateRunsettings -eq "y" -or $updateRunsettings -eq "Y") {
    $xml = @"
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <!-- Azure AD App Registration -->
      <AzureAd__Instance>https://login.microsoftonline.com/</AzureAd__Instance>
      <AzureAd__TenantId>$tenantId</AzureAd__TenantId>
      <AzureAd__ClientId>$clientId</AzureAd__ClientId>
      <AzureAd__ClientSecret>$clientSecret</AzureAd__ClientSecret>
      <AzureAd__ClientCredentials__0__SourceType>ClientSecret</AzureAd__ClientCredentials__0__SourceType>
      <AzureAd__ClientCredentials__0__ClientSecret>$clientSecret</AzureAd__ClientCredentials__0__ClientSecret>

      <!-- Teams Service URL -->
      <TEST_SERVICEURL>https://smba.trafficmanager.net/teams/</TEST_SERVICEURL>

      <!-- Core test identifiers -->
      <TEST_CONVERSATIONID>$conversationId</TEST_CONVERSATIONID>
      <TEST_USER_ID>$userMri</TEST_USER_ID>
      <TEST_TEAMID>$(if ($teamInternalId) { $teamInternalId } else { $teamGroupId })</TEST_TEAMID>
      <TEST_CHANNELID>$($selectedChannel.id)</TEST_CHANNELID>
      <TEST_MEETINGID>$meetingId</TEST_MEETINGID>
      <TEST_TENANTID>$tenantId</TEST_TENANTID>

      <!-- Agentic identity (optional) -->
      <TEST_AGENTIC_APPID></TEST_AGENTIC_APPID>
      <TEST_AGENTIC_USERID></TEST_AGENTIC_USERID>

      <!-- Optional -->
      <TEST_USER_ID_2>$userMri2</TEST_USER_ID_2>
      <TEST_CONNECTION_NAME></TEST_CONNECTION_NAME>
      <TEST_OPERATION_ID></TEST_OPERATION_ID>
    </EnvironmentVariables>
  </RunConfiguration>
</RunSettings>
"@

    $xml | Set-Content -Path $runSettingsPath -Encoding UTF8
    Write-Host "`nWrote $runSettingsPath" -ForegroundColor Green
} else {
    Write-Host "`nSkipped writing runsettings. Copy values manually."
}

# --- Notes ---
Write-Host "`n=== Notes ===" -ForegroundColor Yellow
Write-Host "  - TEST_CONVERSATIONID is set to the selected channel. For 1:1 bot conversations,"
Write-Host "    install the bot in the team and capture the conversation ID from the conversationUpdate event."
Write-Host "  - TEST_USER_ID uses the '29:<aad-object-id>' MRI format."
if (-not $meetingId) {
    Write-Host "  - TEST_MEETINGID was not set. Create a meeting manually or grant OnlineMeetings.ReadWrite.All."
}
Write-Host "  - Remember to add integration.runsettings to .gitignore (it contains secrets).`n"
