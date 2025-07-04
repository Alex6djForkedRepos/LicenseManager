@echo off
rem
rem --output "C:\Path\publish\"
rem
dotnet publish LicenseManagerX\LicenseManagerX.csproj --property:PublishProfile=FolderProfile
dotnet publish LicenseManagerX.Console\LicenseManagerX.Console.csproj --property:PublishProfile=FolderProfile
dotnet publish LicenseManagerX_Example\LicenseManagerX_Example.csproj --property:PublishProfile=FolderProfile
