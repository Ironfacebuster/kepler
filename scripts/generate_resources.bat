@echo off

if "%1" == "" (
    <nul set /p ="debug" > src/Resources/type.txt
) else (
    <nul set /p ="%1" > src/Resources/type.txt
)

<nul set /p ="%date% %time%" > src/Resources/buildtime.txt

echo.