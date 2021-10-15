@echo off
setlocal EnableDelayedExpansion 

set FAILED=0
set PASSED=0

echo.

for %%f in ("%~dp0..\tests\*.kep") do (

    setlocal
    for %%i in ("%%f") do (
        rem echo file without newline
        <nul set /p ="%%~ni..."
    )

    set "RESULT="
    
    rem do command and save output to a temporary file
    "build/VS_PUBLISH_OUTPUT/kepler.exe" --file "%%f">%%f_result.txt
    set /P RESULT=<%%f_result.txt
    del "%%f_result.txt"

    rem if the command doesn't print "SUCCESS" it failed!
    rem this also handles other errors, such as exiting without printing, etc.
    if "!RESULT!"=="SUCCESS" (
        echo PASSED
        set /A PASSED=PASSED+1
    ) else (
        echo FAILED
        set /A FAILED=FAILED+1
    )
)

echo.
echo =RESULTS================
echo.

set /A TOTAL=PASSED+FAILED

echo Total Tests: !TOTAL!
echo.

echo Pass/Fail: !PASSED!/!FAILED!

set /A f_passed=PASSED*100
set /A PERCENT=f_passed/TOTAL

echo Percent Completion: !PERCENT!%% 
echo.
echo ========================

rem if failed is greater than zero, exit with an error 
if !FAILED! gtr 0 (
    echo FAILING
    exit /B -1
)

rem otherwise exit without error
echo PASSING
exit /B 0