@PowerShell -NonInteractive -NoProfile -ExecutionPolicy Unrestricted -Command "& {.\build\cix.ps1; exit $LastExitCode }" 
@echo From Cmd.exe: cix.ps1 exited with exit code %errorlevel%
exit %errorlevel%