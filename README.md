# Texter

A floating text editor that lives in your system tray. Open it with a hotkey, write, and copy the result to the clipboard in one keystroke. Built for use with **Grammarly desktop**.

## Requirements

- Windows 10/11
- .NET Framework 4.0 (pre-installed on all modern Windows)

## Build & run

```bat
build.bat   ← compiles Texter.exe (one-time)
run.bat     ← launches the app
```

Or compile manually:

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /target:winexe /optimize+ /out:Texter.exe ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  Texter.cs
```

## Usage

Texter runs silently in the system tray. Double-click the tray icon or press the hotkey to open the window.

| Key | Action |
|-----|--------|
| `Ctrl+Shift+5` | Open / hide the window |
| `Enter` | New line |
| `Shift+Enter` | Copy text to clipboard, clear, and close |
| `Esc` | Close without copying |

All shortcuts are configurable — right-click the tray icon → **Settings**.

## Grammarly

The editor uses the native **RichEdit 5.0** control (`msftedit.dll`), the same one used by WordPad. Grammarly desktop detects it automatically and shows its suggestion bubble when the window is open.

## Settings

Right-click the tray icon → **Settings** to change:

- **Monitor** — open on the active monitor (where your cursor is) or always on the primary monitor
- **Hotkeys** — remap open/hide, copy & close, and cancel to any key combination

Settings are saved to `%APPDATA%\Texter\settings.txt`.
