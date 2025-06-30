@echo off
rem
rem --output "C:\Path\publish\"
rem
dotnet publish LicenseManager_12noon\LicenseManager_12noon.csproj --property:PublishProfile=FolderProfile
dotnet publish LicenseManagerX.Console\LicenseManagerX.Console.csproj --property:PublishProfile=FolderProfile
dotnet publish LicenseManager_ClientExample\LicenseManager_ClientExample.csproj --property:PublishProfile=FolderProfile
