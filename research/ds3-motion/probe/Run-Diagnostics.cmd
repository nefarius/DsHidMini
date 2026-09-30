@echo off
setlocal
cd /d "%~dp0"

echo DsHidMini DS3 diagnostic capture
echo.
echo Keep only the controller being tested connected.
echo Follow each positioning prompt and press Enter when ready.
echo.

if not exist "captures" mkdir "captures"
"%~dp0Ds3MotionProbe.exe" --name reported-controller --interactive --out "%~dp0captures"

echo.
if errorlevel 1 (
    echo The diagnostic exited with an error. Keep this window visible and
    echo include a screenshot when reporting the problem.
) else (
    echo Capture complete. Send the TXT and CSV files from:
    echo %~dp0captures
)
echo.
pause
