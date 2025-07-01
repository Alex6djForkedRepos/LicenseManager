@echo off

setlocal

:: Prompt for version number if not passed as an argument
if "%~1" == "" (
    echo Usage: %~nx0 ^<version^>
    exit /b 1
)

set VERSION=%~1

echo.
echo Push commits...

:: Push all commits to origin
git push origin

echo.
echo Create tag...

:: Create an annotated tag with message
git tag -a v%VERSION% -m "Release %VERSION%"

echo.
echo Push tag...

:: Push the tag to origin
git push origin v%VERSION%

echo.
echo Successfully created and pushed tag: v%VERSION%
endlocal
