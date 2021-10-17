@echo off

rem create resources folder if it doesn't exist already
if not exist "src/Resources" mkdir "src/Resources"

if "%1" == "" (
    <nul set /p ="debug" > src/Resources/type.txt
) else (
    <nul set /p ="%1" > src/Resources/type.txt
)

<nul set /p ="%date% %time%" > src/Resources/buildtime.txt

echo.