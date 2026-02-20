# Monitoring ProcessMonitor Logs

## Log File Location

The application logs are written to:
```
C:\Users\<YourUsername>\AppData\Roaming\WheelOverlay\logs.txt
```

## How to Monitor Logs in Real-Time

### Option 1: Using PowerShell (Recommended)
Open PowerShell and run:
```powershell
Get-Content "$env:APPDATA\WheelOverlay\logs.txt" -Wait -Tail 20
```

This will show the last 20 lines and continuously update as new logs are written.

### Option 2: Using a Text Editor
1. Open the logs.txt file in a text editor that supports auto-refresh (like Notepad++, VS Code, or Sublime Text)
2. Enable auto-refresh/auto-reload in your editor
3. The file will update as the application writes new logs

### Option 3: Using Tail for Windows
If you have Git Bash or WSL installed:
```bash
tail -f /c/Users/<YourUsername>/AppData/Roaming/WheelOverlay/logs.txt
```

## What to Look For

When testing ProcessMonitor, you should see logs like:

### When Starting the Application
```
[INFO] ProcessMonitor.Start: Initial check, isRunning=False, path=C:\Windows\notepad.exe
[INFO] ProcessMonitor.Start: Creating Timer with interval 1000ms
[INFO] ProcessMonitor.Start: Timer created successfully
```

### Every Second (Timer Ticks)
```
[INFO] ProcessMonitor.OnTimerTick: Timer fired at 14:23:45.123
[INFO] CheckTargetApplication: isRunning=False, _targetIsRunning=False
[INFO] IsExecutableRunning: Checking for C:\Windows\notepad.exe
[INFO] IsExecutableRunning: Found 234 total processes
[INFO] IsExecutableRunning: No match found. Checked 180 processes, skipped 54
```

### When Notepad Starts
```
[INFO] ProcessMonitor.OnTimerTick: Timer fired at 14:23:46.124
[INFO] CheckTargetApplication: isRunning=True, _targetIsRunning=False
[INFO] IsExecutableRunning: Checking for C:\Windows\notepad.exe
[INFO] IsExecutableRunning: Found 235 total processes
[INFO] IsExecutableRunning: MATCH FOUND! Process: notepad, Path: C:\Windows\notepad.exe
[INFO] CheckTargetApplication: State changed, firing event with True
```

### When Notepad Closes
```
[INFO] ProcessMonitor.OnTimerTick: Timer fired at 14:23:50.125
[INFO] CheckTargetApplication: isRunning=False, _targetIsRunning=True
[INFO] IsExecutableRunning: Checking for C:\Windows\notepad.exe
[INFO] IsExecutableRunning: Found 234 total processes
[INFO] IsExecutableRunning: No match found. Checked 180 processes, skipped 54
[INFO] CheckTargetApplication: State changed, firing event with False
```

## Troubleshooting

### If you don't see timer tick messages:
1. The timer is not firing - this is the bug we're trying to fix
2. Check if the ProcessMonitor was created and started
3. Look for any error messages in the logs

### If you see timer ticks but no match when notepad is running:
1. The path might not match exactly
2. Check what path notepad is actually running from
3. Look at the "IsExecutableRunning" logs to see what paths are being compared

### If the log file doesn't exist:
1. The application hasn't started yet
2. There's a permissions issue creating the log directory
3. Check the application is actually running (look in Task Manager)

## Testing Steps

1. **Clear the log file** (optional, to start fresh):
   ```powershell
   Clear-Content "$env:APPDATA\WheelOverlay\logs.txt"
   ```

2. **Start monitoring the logs**:
   ```powershell
   Get-Content "$env:APPDATA\WheelOverlay\logs.txt" -Wait -Tail 20
   ```

3. **Start WheelOverlay**:
   ```powershell
   .\WheelOverlay\bin\Debug\net10.0-windows\WheelOverlay.exe
   ```

4. **Configure the target executable**:
   - Right-click system tray icon → Settings
   - Browse and select `C:\Windows\notepad.exe` or `C:\Windows\System32\notepad.exe`
   - Save settings

5. **Watch the logs** - you should see:
   - ProcessMonitor starting
   - Timer being created
   - Timer ticks every second

6. **Launch notepad** and watch for:
   - "MATCH FOUND!" message
   - "State changed, firing event with True"

7. **Close notepad** and watch for:
   - "No match found" message
   - "State changed, firing event with False"

## Expected Behavior

- Timer should fire **every 1 second** (1000ms)
- When notepad starts, the **next timer tick** (within 1 second) should detect it
- When notepad closes, the **next timer tick** (within 1 second) should detect it
- The overlay window should show/hide accordingly
