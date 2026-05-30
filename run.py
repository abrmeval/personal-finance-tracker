import subprocess
import os

# Base directory is wherever this script lives
BASE_DIR = os.path.dirname(os.path.abspath(__file__))

# Working directories for each app
frontend_dir = os.path.join(BASE_DIR, "frontend")
backend_dir  = os.path.join(BASE_DIR, "backend")

# PowerShell scripts that contain the commands for each app
frontend_ps1 = os.path.join(BASE_DIR, "run_frontend.ps1")
backend_ps1  = os.path.join(BASE_DIR, "run_backend.ps1")

# Open Windows Terminal with two tabs, one for each app.
# The ; between the two wt commands is the wt tab separator (not PowerShell).
# -NoExit keeps the tab open after the script finishes so you can read the logs.
# -File runs the .ps1 script instead of an inline command, avoiding ; conflicts.
subprocess.Popen(
    f'wt --title "Frontend" --startingDirectory "{frontend_dir}" powershell -NoExit -File "{frontend_ps1}" ; '
    f'new-tab --title "Backend" --startingDirectory "{backend_dir}" powershell -NoExit -File "{backend_ps1}"',
    shell=True  # Required so Windows parses the wt command like a terminal would
)

print("Frontend and backend are running in separate tabs.")