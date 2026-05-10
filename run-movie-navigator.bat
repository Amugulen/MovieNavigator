@echo off
setlocal

cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] .NET SDK was not found.
    echo Install .NET 8 SDK or newer, then run this file again.
    pause
    exit /b 1
)

echo Starting Movie Navigator...
dotnet run --project src\MovieNavigator.App\MovieNavigator.App.csproj

if errorlevel 1 (
    echo.
    echo [ERROR] Movie Navigator failed to start.
    echo Check the messages above for details.
    pause
    exit /b 1
)

endlocal
