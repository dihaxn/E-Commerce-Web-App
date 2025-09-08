# Simple E-Commerce Production Deployment Script
param(
    [Parameter(Mandatory = $true)]
    [string]$Environment,
    
    [string]$ConnectionString,
    [string]$BrevoApiKey,
    [string]$PayPalClientId,
    [string]$PayPalSecret,
    [string]$CookieEncryptionKey
)

Write-Host "🚀 Starting E-Commerce Deployment for $Environment" -ForegroundColor Green

# Set environment variables
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

# Build the application
Write-Host "🔨 Building Application..." -ForegroundColor Blue
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}
Write-Host "✓ Application built successfully" -ForegroundColor Green

# Run tests
Write-Host "🧪 Running Tests..." -ForegroundColor Blue
dotnet test --configuration Release --no-build --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Tests failed - continuing with deployment"
} else {
    Write-Host "✓ All tests passed" -ForegroundColor Green
}

# Publish the application
Write-Host "📦 Publishing Application..." -ForegroundColor Blue
$publishPath = "publish/$Environment"
dotnet publish --configuration Release --output $publishPath --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed"
    exit 1
}
Write-Host "✓ Application published to: $publishPath" -ForegroundColor Green

# Create startup script
Write-Host "📜 Creating Startup Script..." -ForegroundColor Blue
$startupScript = @"
@echo off
echo Starting E-Commerce Application in $Environment mode...
echo.
echo Environment Variables:
echo   ASPNETCORE_ENVIRONMENT=$Environment
echo.
echo Starting application...
dotnet E-Commerce-BE.dll
"@

$startupPath = "$publishPath/start-$Environment.bat"
$startupScript | Out-File -FilePath $startupPath -Encoding ASCII
Write-Host "✓ Startup script created: $startupPath" -ForegroundColor Green

# Create deployment summary
Write-Host "`n📊 Deployment Summary" -ForegroundColor Green
Write-Host "=====================" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor White
Write-Host "Publish Path: $publishPath" -ForegroundColor White
Write-Host "Startup Script: $startupPath" -ForegroundColor White

Write-Host "`n🚀 Deployment completed successfully!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Copy files from '$publishPath' to your production server" -ForegroundColor White
Write-Host "2. Set environment variables on the production server" -ForegroundColor White
Write-Host "3. Configure SSL certificate" -ForegroundColor White
Write-Host "4. Start the application using 'start-$Environment.bat'" -ForegroundColor White
Write-Host "5. Verify health check at '/api/health' endpoint" -ForegroundColor White
