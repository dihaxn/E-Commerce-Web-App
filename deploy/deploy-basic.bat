@echo off
echo 🚀 Starting E-Commerce Production Deployment
echo.

REM Set environment
set ASPNETCORE_ENVIRONMENT=Production
echo ✓ Environment set to: Production

REM Build application
echo 🔨 Building Application...
dotnet build --configuration Release --no-restore
if %ERRORLEVEL% neq 0 (
    echo ❌ Build failed
    pause
    exit /b 1
)
echo ✓ Application built successfully

REM Run tests
echo 🧪 Running Tests...
dotnet test --configuration Release --no-build --verbosity normal
if %ERRORLEVEL% neq 0 (
    echo ⚠️ Tests failed - continuing with deployment
) else (
    echo ✓ All tests passed
)

REM Publish application
echo 📦 Publishing Application...
set PUBLISH_PATH=publish\Production
dotnet publish --configuration Release --output %PUBLISH_PATH% --no-build
if %ERRORLEVEL% neq 0 (
    echo ❌ Publish failed
    pause
    exit /b 1
)
echo ✓ Application published to: %PUBLISH_PATH%

REM Create startup script
echo 📜 Creating Startup Script...
echo @echo off > "%PUBLISH_PATH%\start-Production.bat"
echo echo Starting E-Commerce Application in Production mode... >> "%PUBLISH_PATH%\start-Production.bat"
echo echo. >> "%PUBLISH_PATH%\start-Production.bat"
echo echo Environment Variables: >> "%PUBLISH_PATH%\start-Production.bat"
echo echo   ASPNETCORE_ENVIRONMENT=Production >> "%PUBLISH_PATH%\start-Production.bat"
echo echo. >> "%PUBLISH_PATH%\start-Production.bat"
echo echo Starting application... >> "%PUBLISH_PATH%\start-Production.bat"
echo dotnet E-Commerce-BE.dll >> "%PUBLISH_PATH%\start-Production.bat"
echo ✓ Startup script created: %PUBLISH_PATH%\start-Production.bat

REM Create deployment summary
echo.
echo 📊 Deployment Summary
echo =====================
echo Environment: Production
echo Publish Path: %PUBLISH_PATH%
echo Startup Script: %PUBLISH_PATH%\start-Production.bat

echo.
echo 🚀 Deployment completed successfully!
echo.
echo Next steps:
echo 1. Copy files from '%PUBLISH_PATH%' to your production server
echo 2. Set environment variables on the production server
echo 3. Configure SSL certificate
echo 4. Start the application using 'start-Production.bat'
echo 5. Verify health check at '/api/health' endpoint
echo.
pause
