@echo off
setlocal

:: === AutoClicker Build Script ===

:: Check dotnet
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] dotnet not found. Please install .NET 8 SDK
    echo Download: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo [1/3] Cleaning...
if exist "AutoClicker\bin" rmdir /s /q "AutoClicker\bin"
if exist "AutoClicker\obj" rmdir /s /q "AutoClicker\obj"
if exist "dist" rmdir /s /q "dist"

echo [2/3] Publishing...
dotnet publish AutoClicker\AutoClicker.csproj -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true /p:IncludeNativeLibrariesForSelfExtract=true -o dist

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Build failed. Check errors above.
    pause
    exit /b 1
)

echo [3/3] Cleaning up...
del /q dist\*.pdb 2>nul

echo.
echo ============================================
echo   Build complete!
echo   Output: %cd%\dist\AutoClicker.exe
echo ============================================
echo.
pause
