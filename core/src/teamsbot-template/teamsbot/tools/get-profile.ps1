# PowerShell script to reset Azure AD app registration credentials
# This script checks Azure CLI availability, login status, and resets app credentials

param(
    [Parameter()]
    [string]$AppId
)

# Function to check if Azure CLI is installed
function Test-AzureCLI {
    try {
        $azVersion = az version --output json 2>$null | ConvertFrom-Json
        if ($azVersion) {
            Write-Host "✓ Azure CLI is installed (version: $($azVersion.'azure-cli'))" -ForegroundColor Green
            return $true
        }
    }
    catch {
        Write-Error "Azure CLI is not installed or not accessible. Please install Azure CLI first."
        Write-Host "Download from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" -ForegroundColor Yellow
        return $false
    }
}

# Function to check if user is logged in to Azure CLI
function Test-AzureLogin {
    try {
        $account = az account show --output json 2>$null | ConvertFrom-Json
        if ($account) {
            Write-Host "✓ Logged in to Azure as: $($account.user.name)" -ForegroundColor Green
            Write-Host "  Tenant: $($account.tenantId)" -ForegroundColor Gray
            Write-Host "  Subscription: $($account.name)" -ForegroundColor Gray
            return $account
        }
    }
    catch {
        Write-Error "Not logged in to Azure CLI. Please run 'az login' first."
        return $null
    }
}

# Function to validate App ID format (GUID)
function Test-AppIdFormat {
    param([string]$AppId)
    
    if ([string]::IsNullOrWhiteSpace($AppId)) {
        return $false
    }
    
    try {
        $guid = [System.Guid]::Parse($AppId)
        return $true
    }
    catch {
        return $false
    }
}

# Function to get app display name
function Get-AppDisplayName {
    param([string]$AppId)
    
    try {
        Write-Host "Getting app display name..." -ForegroundColor Gray
        
        # Use --only-show-errors to reduce output noise and potential hanging
        $appInfo = az ad app show --id $AppId --output json --only-show-errors
        
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($appInfo)) {
            Write-Warning "Could not retrieve app display name. Using App ID as filename."
            return $AppId
        }
        
        $appData = $appInfo | ConvertFrom-Json
        $displayName = $appData.displayName
        
        if ([string]::IsNullOrWhiteSpace($displayName)) {
            Write-Warning "App display name is empty. Using App ID as filename."
            return $AppId
        }
        
        # Clean the display name for use as filename - remove invalid characters
        $cleanName = $displayName -replace '[\\/:*?"<>|]', '_' -replace '\s+', '_'
        Write-Host "✓ App display name: '$displayName' -> '$cleanName.json'" -ForegroundColor Green
        
        return $cleanName
    }
    catch {
        Write-Warning "Error getting app display name: $($_.Exception.Message). Using App ID as filename."
        return $AppId
    }
}

# Function to reset app credentials
function Reset-AppCredentials {
    param(
        [string]$AppId,
        [string]$TenantId
    )
    
    try {
        Write-Host "Resetting credentials for app: $AppId" -ForegroundColor Yellow
        
        # Reset the app credentials
        $result = az ad app credential reset --id $AppId --output json 2>$null
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to reset credentials. Please check if the App ID is correct and you have permissions."
            return $null
        }
        
        $credentialInfo = $result | ConvertFrom-Json
        
        # Create the output object with Teams environment variable format
        $output = @{
            "CLIENT_ID" = $AppId
            "TENANT_ID" = $TenantId
            "CLIENT_SECRET" = $credentialInfo.password
        }
        
        Write-Host "✓ Credentials reset successfully!" -ForegroundColor Green
        return $output
    }
    catch {
        Write-Error "Error resetting app credentials: $($_.Exception.Message)"
        return $null
    }
}

# Main execution
Write-Host "=== Azure AD App Registration Credential Reset ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if Azure CLI is installed
if (-not (Test-AzureCLI)) {
    exit 1
}

# Step 2: Check if logged in to Azure
$account = Test-AzureLogin
if (-not $account) {
    Write-Host "Please run 'az login' to authenticate with Azure." -ForegroundColor Yellow
    exit 1
}

# Step 3: Get App ID if not provided
if ([string]::IsNullOrWhiteSpace($AppId)) {
    do {
        $AppId = Read-Host "Enter the App Registration ID (GUID)"
        
        if (-not (Test-AppIdFormat -AppId $AppId)) {
            Write-Host "Invalid App ID format. Please enter a valid GUID." -ForegroundColor Red
        }
    } while (-not (Test-AppIdFormat -AppId $AppId))
}
else {
    if (-not (Test-AppIdFormat -AppId $AppId)) {
        Write-Error "Invalid App ID format provided: $AppId"
        exit 1
    }
}

Write-Host ""

# Step 4: Get app display name for output filename
$appDisplayName = Get-AppDisplayName -AppId $AppId
$outputFile = "$appDisplayName.json"

# Step 5: Reset app credentials
$credentials = Reset-AppCredentials -AppId $AppId -TenantId $account.tenantId

if (-not $credentials) {
    exit 1
}

# Step 6: Output to JSON file
try {
    # Create formatted JSON with complete launchSettings.json profile format
    $jsonOutput = @"
{
  "$appDisplayName": {
    "commandName": "Project",
    "launchBrowser": false,
    "environmentVariables": {
      "ASPNETCORE_ENVIRONMENT": "Development",
      "CLIENT_ID": "$($credentials."CLIENT_ID")",
      "TENANT_ID": "$($credentials."TENANT_ID")",
      "CLIENT_SECRET": "$($credentials."CLIENT_SECRET")"
    },
    "applicationUrl": "http://localhost:3978"
  }
}
"@
    
    $jsonOutput | Out-File -FilePath $outputFile -Encoding UTF8
    
    Write-Host ""
    Write-Host "=== Credentials Reset Complete ===" -ForegroundColor Green
    Write-Host "CLIENT_ID: $($credentials."CLIENT_ID")" -ForegroundColor Gray
    Write-Host "TENANT_ID: $($credentials."TENANT_ID")" -ForegroundColor Gray
    Write-Host "CLIENT_SECRET: $($credentials."CLIENT_SECRET".Substring(0, 8))..." -ForegroundColor Gray
    Write-Host ""
    Write-Host "✓ Credentials saved to: $outputFile" -ForegroundColor Green
    
    # Display the JSON content
    Write-Host ""
    Write-Host "JSON Output:" -ForegroundColor Cyan
    Write-Host $jsonOutput -ForegroundColor White
}
catch {
    Write-Error "Failed to save credentials to file: $($_.Exception.Message)"
    
    # Still display the credentials if file save failed
    Write-Host ""
    Write-Host "Credentials (file save failed):" -ForegroundColor Yellow
    Write-Host "CLIENT_ID: $($credentials."CLIENT_ID")"
    Write-Host "TENANT_ID: $($credentials."TENANT_ID")"
    Write-Host "CLIENT_SECRET: $($credentials."CLIENT_SECRET")"
}

Write-Host ""
Write-Host "Script completed successfully!" -ForegroundColor Green
