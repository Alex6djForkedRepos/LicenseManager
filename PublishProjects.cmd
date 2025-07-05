@echo off
rem
rem --output "C:\Path\publish\"
rem

setlocal

:: Prompt for version number if not passed as an argument
if "%~1" == "" (
    echo Usage: %~nx0 ^<version^>
    exit /b 1
)

set VERSION=%~1

dotnet pack LicenseManager_12noon.Client\LicenseManager_12noon.Client.csproj --nologo -p:Version=%VERSION% --configuration Release --runtime win-x64 --output C:\VSIntermediate\LicenseManagerX\publish\
dotnet publish LicenseManagerX\LicenseManagerX.csproj --property:PublishProfile=FolderProfile
dotnet publish LicenseManagerX.Console\LicenseManagerX.Console.csproj --property:PublishProfile=FolderProfile
dotnet publish LicenseManagerX_Example\LicenseManagerX_Example.csproj --property:PublishProfile=FolderProfile

endlocal
