[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $DeploymentToken
)

$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'BlazoradeStaticPagesDev\BlazoradeStaticPagesDev.csproj'
$publishOutputPath = Join-Path $PSScriptRoot 'BlazoradeStaticPagesDev\bin\Release\net10.0\publish'
$staticContentPath = Join-Path $publishOutputPath 'wwwroot'
$staticWebAppUrl = 'https://thankful-coast-04f2b8403.7.azurestaticapps.net/'
$subscriptionId = '7b9bcb59-8ea7-49a6-8f69-ec8418acab78'
$resourceGroupName = 'Blazorade-StaticPages'
$resourceName = 'Blazorade-StaticPages-Dev'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "The Blazor WebAssembly project was not found at '$projectPath'."
}

$swaCommand = Get-Command swa -ErrorAction SilentlyContinue
if ($null -eq $swaCommand) {
    throw "The Static Web Apps CLI ('swa') is required but was not found on PATH. Install it separately with 'npm install -g @azure/static-web-apps-cli', then run this script again."
}

Write-Host 'Cleaning the previous publish output...'
if (Test-Path -LiteralPath $publishOutputPath) {
    Remove-Item -LiteralPath $publishOutputPath -Recurse -Force
}

Write-Host 'Publishing BlazoradeStaticPagesDev in Release mode...'
dotnet publish $projectPath `
    --configuration Release `
    --output $publishOutputPath

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host 'Deploying to Azure Static Web Apps...'
& $swaCommand.Source deploy $staticContentPath `
    --deployment-token $DeploymentToken `
    --swa-config-location $staticContentPath `
    --subscription-id $subscriptionId `
    --resource-group $resourceGroupName `
    --app-name $resourceName `
    --env production

if ($LASTEXITCODE -ne 0) {
    throw "Static Web Apps deployment failed with exit code $LASTEXITCODE."
}

Write-Host 'Deployment completed successfully.'
Write-Host "Static Web App: $resourceName"
Write-Host "Resource group: $resourceGroupName"
Write-Host "Subscription: $subscriptionId"
Write-Host "URL: $staticWebAppUrl"
