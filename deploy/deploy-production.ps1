# Production Deployment Script for E-Commerce Application
# This script handles production deployment with security validation

param(
    [Parameter(Mandatory = $true)]
    [string]$Environment,
    
    [Parameter(Mandatory = $false)]
    [string]$ConnectionString,
    
    [Parameter(Mandatory = $false)]
    [string]$BrevoApiKey,
    
    [Parameter(Mandatory = $false)]
    [string]$PayPalClientId,
    
    [Parameter(Mandatory = $false)]
    [string]$PayPalSecret,
    
    [Parameter(Mandatory = $false)]
    [string]$CookieEncryptionKey
)

# Set error action preference
$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting Production Deployment for E-Commerce Application" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Yellow

# Validate environment
if ($Environment -notin @("Production", "Staging")) {
    Write-Error "Invalid environment. Must be 'Production' or 'Staging'"
    exit 1
}

# Check if running as administrator (for production deployment)
if ($Environment -eq "Production" -and -not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Warning "Production deployment should be run as Administrator"
    $continue = Read-Host "Continue anyway? (y/N)"
    if ($continue -ne "y" -and $continue -ne "Y") {
        exit 1
    }
}

# Set environment variables
Write-Host "🔧 Setting Environment Variables..." -ForegroundColor Blue

if ($ConnectionString) {
    $env:ConnectionStrings__DefaultConnection = $ConnectionString
    Write-Host "✓ Database connection string set" -ForegroundColor Green
}

if ($BrevoApiKey) {
    $env:BrevoSettings__ApiKey = $BrevoApiKey
    Write-Host "✓ Brevo API key set" -ForegroundColor Green
}

if ($PayPalClientId) {
    $env:PayPalSettings__ClientId = $PayPalClientId
    Write-Host "✓ PayPal client ID set" -ForegroundColor Green
}

if ($PayPalSecret) {
    $env:PayPalSettings__Secret = $PayPalSecret
    Write-Host "✓ PayPal secret set" -ForegroundColor Green
}

if ($CookieEncryptionKey) {
    $env:CookieEncryptionKey = $CookieEncryptionKey
    Write-Host "✓ Cookie encryption key set" -ForegroundColor Green
}

# Set ASP.NET Core environment
$env:ASPNETCORE_ENVIRONMENT = $Environment
Write-Host "✓ ASP.NET Core environment set to: $Environment" -ForegroundColor Green

# Validate required environment variables
Write-Host "🔍 Validating Environment Configuration..." -ForegroundColor Blue

$requiredVars = @(
    "ConnectionStrings__DefaultConnection",
    "BrevoSettings__ApiKey",
    "PayPalSettings__ClientId",
    "PayPalSettings__Secret",
    "CookieEncryptionKey"
)

$missingVars = @()
foreach ($var in $requiredVars) {
    if (-not (Get-Variable -Name $var -ErrorAction SilentlyContinue) -and -not (Get-Variable -Name "env:$var" -ErrorAction SilentlyContinue)) {
        $missingVars += $var
    }
}

if ($missingVars.Count -gt 0) {
    Write-Warning "Missing environment variables:"
    foreach ($var in $missingVars) {
        Write-Warning "  - $var"
    }
    Write-Warning "These can be set via parameters or environment variables"
}

# Build the application
Write-Host "🔨 Building Application..." -ForegroundColor Blue
try {
    dotnet build --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    Write-Host "✓ Application built successfully" -ForegroundColor Green
}
catch {
    Write-Error "Build failed: $_"
    exit 1
}

# Run tests
Write-Host "🧪 Running Tests..." -ForegroundColor Blue
try {
    dotnet test --configuration Release --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed"
    }
    Write-Host "✓ All tests passed" -ForegroundColor Green
}
catch {
    Write-Error "Tests failed: $_"
    exit 1
}

# Publish the application
Write-Host "📦 Publishing Application..." -ForegroundColor Blue
$publishPath = "publish/$Environment"
try {
    dotnet publish --configuration Release --output $publishPath --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed"
    }
    Write-Host "✓ Application published to: $publishPath" -ForegroundColor Green
}
catch {
    Write-Error "Publish failed: $_"
    exit 1
}

# Create production configuration
Write-Host "⚙️ Creating Production Configuration..." -ForegroundColor Blue
$configPath = "$publishPath/appsettings.$Environment.json"
if (Test-Path $configPath) {
    Write-Host "✓ Production configuration exists" -ForegroundColor Green
}
else {
    Write-Warning "Production configuration not found: $configPath"
}

# Validate SSL certificate (if HTTPS is enabled)
if ($Environment -eq "Production") {
    Write-Host "🔒 Validating SSL Configuration..." -ForegroundColor Blue
    # This would check for valid SSL certificates
    Write-Host "⚠️ SSL certificate validation not implemented - verify manually" -ForegroundColor Yellow
}

# Create deployment manifest
Write-Host "📋 Creating Deployment Manifest..." -ForegroundColor Blue
$manifest = @{
    DeploymentId         = [System.Guid]::NewGuid().ToString()
    Timestamp            = Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC"
    Environment          = $Environment
    Version              = "1.0.0"
    BuildConfiguration   = "Release"
    PublishPath          = $publishPath
    EnvironmentVariables = @{
        ASPNETCORE_ENVIRONMENT = $Environment
        ConnectionString       = if ($ConnectionString) { "Set" } else { "Not Set" }
        BrevoApiKey            = if ($BrevoApiKey) { "Set" } else { "Not Set" }
        PayPalClientId         = if ($PayPalClientId) { "Set" } else { "Not Set" }
        PayPalSecret           = if ($PayPalSecret) { "Set" } else { "Not Set" }
        CookieEncryptionKey    = if ($CookieEncryptionKey) { "Set" } else { "Not Set" }
    }
}

$manifestPath = "$publishPath/deployment-manifest.json"
$manifest | ConvertTo-Json -Depth 10 | Out-File -FilePath $manifestPath -Encoding UTF8
Write-Host "✓ Deployment manifest created: $manifestPath" -ForegroundColor Green

# Security validation
Write-Host "🛡️ Performing Security Validation..." -ForegroundColor Blue

# Check for hardcoded secrets
$secretsFound = @()
$filesToCheck = @(
    "$publishPath/appsettings.json",
    "$publishPath/appsettings.$Environment.json"
)

foreach ($file in $filesToCheck) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        if ($content -match '"ApiKey":\s*"[^"]*"') {
            $secretsFound += "API key found in $file"
        }
        if ($content -match '"Secret":\s*"[^"]*"') {
            $secretsFound += "Secret found in $file"
        }
        if ($content -match '"Password":\s*"[^"]*"') {
            $secretsFound += "Password found in $file"
        }
    }
}

if ($secretsFound.Count -gt 0) {
    Write-Warning "Potential secrets found in configuration files:"
    foreach ($secret in $secretsFound) {
        Write-Warning "  - $secret"
    }
    Write-Warning "Review these files before deployment"
}
else {
    Write-Host "✓ No hardcoded secrets found" -ForegroundColor Green
}

# Create startup script
Write-Host "📜 Creating Startup Script..." -ForegroundColor Blue
$startupScript = @"
@echo off
echo Starting E-Commerce Application in $Environment mode...
echo.
echo Environment Variables:
echo   ASPNETCORE_ENVIRONMENT=$Environment
echo   ConnectionString=[Set]
echo   BrevoApiKey=[Set]
echo   PayPalClientId=[Set]
echo   PayPalSecret=[Set]
echo   CookieEncryptionKey=[Set]
echo.
echo Starting application...
dotnet E-Commerce-BE.dll
"@

$startupPath = "$publishPath/start-$Environment.bat"
$startupScript | Out-File -FilePath $startupPath -Encoding ASCII
Write-Host "✓ Startup script created: $startupPath" -ForegroundColor Green

# Create deployment summary
Write-Host "📊 Deployment Summary" -ForegroundColor Green
Write-Host "=====================" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor White
Write-Host "Publish Path: $publishPath" -ForegroundColor White
Write-Host "Configuration: $configPath" -ForegroundColor White
Write-Host "Startup Script: $startupPath" -ForegroundColor White
Write-Host "Manifest: $manifestPath" -ForegroundColor White

Write-Host "`n🚀 Deployment completed successfully!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Copy files from '$publishPath' to your production server" -ForegroundColor White
Write-Host "2. Set environment variables on the production server" -ForegroundColor White
Write-Host "3. Configure SSL certificate" -ForegroundColor White
Write-Host "4. Start the application using 'start-$Environment.bat'" -ForegroundColor White
Write-Host "5. Verify health check at '/health' endpoint" -ForegroundColor White

Write-Host "`n⚠️ Security Reminders:" -ForegroundColor Red
Write-Host "- Never commit secrets to source control" -ForegroundColor White
Write-Host "- Use environment variables for sensitive data" -ForegroundColor White
Write-Host "- Enable HTTPS in production" -ForegroundColor White
Write-Host "- Monitor application logs" -ForegroundColor White
Write-Host "- Regular security audits" -ForegroundColor White
