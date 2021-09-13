@echo off
SETLOCAL EnableDelayedExpansion 

set FAILED=0
set PASSED=0

echo.

for %%f in ("%~dp0*.kep") do (
    @REM echo file without newline
    <nul set /p ="%%f..."

    set "RESULT="
    
    @REM do command and save output to a temporary file
    kepler "%%f">%%f_result.txt
    set /P RESULT=<%%f_result.txt
    del "%%f_result.txt"

    @REM if the command resulted in ANY output it failed
    @REM this is assuming that pass results don't print to console
    if "!RESULT!"=="" (
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
echo Pass/Fail: !PASSED!/!FAILED!


set /A TOTAL=PASSED+FAILED
set /A f_passed=PASSED*100
set /A PERCENT=f_passed/TOTAL

echo Percent Completion: !PERCENT!%% 
echo.
echo ========================