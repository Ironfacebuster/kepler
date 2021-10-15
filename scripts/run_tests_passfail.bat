@echo off
setlocal EnableDelayedExpansion 

set FAILED=0
set PASSED=0

echo.

for %%f in ("%~dp0..\tests\*.kep") do (

    setlocal

    set "RESULT="
    
    rem do command and save output to a temporary file
    "build/VS_PUBLISH_OUTPUT/kepler.exe" --file "%%f">%%f_result.txt
    set /P RESULT=<%%f_result.txt
    del "%%f_result.txt"

    rem if the command doesn't print "SUCCESS" it failed!
    rem this also handles other errors, such as exiting without printing, etc.
    if "!RESULT!"=="SUCCESS" (
        set /A PASSED=PASSED+1
    ) else (
        set /A FAILED=FAILED+1
    )
)

rem if failed is greater than zero, exit with an error 
if !FAILED! gtr 0 (
    echo FAILING
    exit /B -1
)

rem otherwise exit without error
echo PASSING
exit /B 0