<p align="center">
  <img src="Assets/WinAirBarLogo.png" alt="WinAirBar floating Windows taskbar menu logo" width="460">
</p>

<h1 align="center">WinAirBar</h1>

<p align="center">
  <strong>A lightweight floating Windows taskbar menu with a local Workspace Control Center, window switching, and app launching for Windows 10 and Windows 11.</strong>
</p>

<p align="center">
  <a href="https://github.com/UncostlyDevs/WinAirBar/releases/latest">Download latest release</a>
  |
  <a href="https://winairbar.com">Website</a>
  |
  <a href="mailto:sag@winairbar.com">Contact</a>
</p>

<p align="center">
  <img alt="Latest release" src="https://img.shields.io/github/v/release/UncostlyDevs/WinAirBar?label=release">
  <img alt="Windows 10 and 11" src="https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4">
  <img alt=".NET 8 WPF" src="https://img.shields.io/badge/.NET-8.0-512BD4">
  <img alt="License" src="https://img.shields.io/github/license/UncostlyDevs/WinAirBar">
</p>

## What Is WinAirBar?

WinAirBar is a small Windows productivity utility that gives you a compact floating taskbar-style menu for switching windows, reopening recent windows, launching pinned or frequent apps, and reaching common system controls without digging through the Start menu or taskbar.

It is built for people who want a cleaner Windows desktop workflow: quick window switching, a fast app launcher, local window history, power controls, volume controls, network shortcuts, and customizable action buttons in one lightweight tray app.

WinAirBar was previously released as **AirBar**. Version 1.2.0 renamed the product, updated the release metadata, and safely migrates existing local settings from `%AppData%\AirBar` to `%AppData%\WinAirBar`. Version 1.3.0 added Workspace Memory, cleaner Windows 11-style UI polish, app-themed dialogs, and stronger workspace restore confidence. Version 1.4.0 adds the Workspace Control Center, visual workspace screens, restore modes, cleanup planning, versions, rules, suggestions, launcher tags, and stronger backup conflict handling.

WinAirBar is passion-built, free, local-first desktop software. The project remains MIT-licensed and does not add paid tiers, account requirements, telemetry, or cloud sync.

## Current Release: v1.4.0

WinAirBar v1.4.0 is the Workspace Control Center release. It keeps the quick Workspace flyout fast while adding a deeper local control surface for previewing, restoring, organizing, backing up, and visually remembering workspaces.

- Open a dedicated Workspace Control Center with workspace cards, details, screen galleries, restore plans, history, rules, suggestions, groups, launcher tags, and backup controls.
- Preview restore plans before acting, including matched windows, windows to launch, missing targets, low-confidence matches, monitor remaps, changed positions, and extra current windows.
- Choose restore modes for Full Restore, Missing Only, Layout Only, and Clean Restore.
- Review Clean Restore cleanup choices before minimizing or closing extra windows. Protected/risky windows are kept by default, and close actions require final confirmation.
- Save local screen memories for workspaces, with larger galleries and small thumbnails in the workspace list.
- Keep versions, timeline history, rules, local suggestions, launcher tags, per-workspace auto-actions, and backup conflict choices readable and local.
- Keep everything local under your user profile: no account, no telemetry, no cloud workspace sync.

## Why Use It?

- Get a floating Windows taskbar menu from a mouse trigger instead of reaching for the main taskbar.
- Switch between active windows from a compact flyout.
- Reopen recent windows and frequently used apps faster.
- Save, preview, restore, and visually recognize workspace layouts before switching projects or returning to a previous setup.
- Keep pinned launch shortcuts close without cluttering the desktop.
- Put power, volume, network, settings, and custom actions in one bottom action bar.
- Use retro Windows-inspired themes alongside modern Windows 10/11 styling.
- Keep data local: no telemetry, no account, no cloud sync, no background service calling home.

## Features

### Floating Taskbar Menu

Open a compact taskbar-style flyout with your configured mouse trigger. The menu is designed for quick repeated use: switch windows, jump into launchers, and get back to work.

### Window Switcher

See open windows in a focused list and bring the one you need forward. WinAirBar is useful as a lightweight window switcher for people who bounce between many apps during the day.

### App Launcher

Launch pinned apps, frequent apps, and configured shortcuts from the same menu. This makes WinAirBar a simple Windows app launcher without turning it into a full search box or plugin platform.

### Recent Window History

Track window history locally so you can reopen or return to recent work more easily. History is stored under your user profile, not uploaded anywhere.

### Bottom Action Bar

Configure five compact quick actions for power, restart, sleep, sign out, lock, volume, sound settings, network, folders, Windows tools, Workspace Memory, and custom launch targets.

### Workspace Control Center

Capture the current window layout, choose a saved workspace, and restore it later with a preview-first plan. The Workspace Control Center shows workspace cards, notes, screen galleries, restore plans, versions, timeline history, rules, suggestions, groups, launcher tags, and backup controls. Restore results are summarized after each restore, so you can tell whether the workspace came back cleanly or needs attention.

### Restore Modes And Cleanup

Use Full Restore, Missing Only, Layout Only, or Clean Restore depending on what you need. Clean Restore shows extra current windows with Keep, Minimize, or Close choices. Risky/protected windows are kept by default, and closing windows requires a final confirmation.

### Workspace Screens

WinAirBar saves local screen memories when you save or update a workspace. The Control Center shows large screen galleries for detail and smaller thumbnails in the workspace list for quick visual recognition.

### Workspace Switcher

Open a desktop-switcher-style overlay with horizontal workspace cards. Click a card to preview/restore, use arrow navigation, press Enter to restore, or Esc to close.

### Rules, Suggestions, Groups, And Tags

Window Rules can exclude apps from capture, protect apps from cleanup, set default cleanup actions, set restore modes, and choose default placement. Local suggestions can recommend a workspace when the same app/window group appears repeatedly. Pinned window groups and launcher tags help organize lighter workflows without needing a full saved layout.

### Backup & Restore

Export selected local data to a normal ZIP with readable JSON files, then preview and import only the sections you choose. Backups can include settings, bottom actions, launcher apps, pinned profiles, window history, workspaces, and workspace snapshots. Imports create a local pre-import backup and ask how to handle conflicts before replacing selected data.

### Safety Prompts

Power actions ask for confirmation before shutdown, restart, sleep, or sign out. Dangerous per-workspace auto-actions require confirmation every time. Custom action slots ask for confirmation before first launch. WinAirBar-owned confirmation and info dialogs use the app theme instead of the default system message-box style.

### Windows Themes

WinAirBar includes theme assets inspired by Windows 11, Windows 10, Windows 7, Windows XP, Windows 9x, Windows 3.1, and pixel-style utility icons.

## Download

Download the latest Windows x64 release from:

https://github.com/UncostlyDevs/WinAirBar/releases/latest

Current v1.4.0 release assets:

```text
WinAirBar-v1.4.0-win-x64.exe
WinAirBar-v1.4.0-win-x64.zip
```

WinAirBar is distributed as a self-contained Windows executable. No installer is required.

Because the executable is currently unsigned, Windows SmartScreen may show a first-run warning. Verify the SHA256 checksum published with the release before running the EXE.

```powershell
Get-FileHash .\WinAirBar-v1.4.0-win-x64.exe -Algorithm SHA256
```

Expected v1.4.0 EXE SHA256:

```text
B9F7F7EFF024893F5071EA72EDE68EA41C0FBC46463056CE5F6E50FB3CA7FC0B
```

Expected v1.4.0 ZIP SHA256:

```text
D1A2902AA87C8585ABAD46A4CB3FCAF2D3EDDAC11957D15678961712E6BE58E1
```

## Requirements

- Windows 10 or Windows 11.
- Windows x64 for the published self-contained release.
- .NET 8 SDK only if you want to build from source.

## Build From Source

Clone the repository, then run:

```powershell
dotnet restore FloatingTaskbarMenu.csproj
dotnet build FloatingTaskbarMenu.csproj
```

Run from source:

```powershell
dotnet run --project FloatingTaskbarMenu.csproj
```

Publish a Windows x64 self-contained build:

```powershell
dotnet publish FloatingTaskbarMenu.csproj -c Release -r win-x64 --self-contained true
```

The published executable is written under:

```text
bin\Release\net8.0-windows\win-x64\publish\
```

## Privacy And Security

- WinAirBar does not collect telemetry.
- WinAirBar does not require a user account.
- WinAirBar does not send your app data to a remote service.
- Settings, launcher data, pinned profiles, logs, window history, workspace data, workspace screenshots, rules, timeline events, and suggestions are stored locally under `%AppData%\WinAirBar`.
- First launch of v1.2.0 copies existing AirBar data from `%AppData%\AirBar` only when WinAirBar data is not already present.
- The old `%AppData%\AirBar` folder is left in place as a backup.
- Autostart uses the current user's Windows Run key only.
- WinAirBar does not require administrator privileges.

Report security issues to:

```text
sag@winairbar.com
```

## Good Fit

WinAirBar may be useful if you searched for:

- Windows taskbar launcher
- floating taskbar menu for Windows
- Windows workspace manager
- Windows workspace restore
- Windows workspace switcher
- Windows 11 app launcher
- Windows window switcher
- taskbar alternative for Windows
- system tray productivity tool
- local-first Windows utility
- lightweight WPF desktop app launcher

WinAirBar is intentionally not a full keyboard search launcher, file indexer, or plugin ecosystem. If you want a keyboard-first Spotlight-style search box, another launcher may fit better. If you want a small floating menu for windows, apps, and system actions, WinAirBar is built for that lane.

## Source Layout

- `App.xaml` / `App.xaml.cs` - application startup, theme loading, and tray icon.
- `Controls/` - flyout controls for windows, launcher, settings, history, and bottom actions.
- `Core/` - window tracking, settings, launcher, history, workspace, backup, theme, migration, and system helper services.
- `Models/` - serializable app and settings models.
- `Styles/` - shared Windows style resources.
- `Windows/` - WinAirBar windows and dialogs.
- `Assets/` - WinAirBar logo, application icon, and theme icons.
- `release/` - packaged release artifacts and checksums.

## Project Info

- Website: https://winairbar.com
- Contact: sag@winairbar.com
- Latest release: https://github.com/UncostlyDevs/WinAirBar/releases/latest
- License: MIT

## Contributing

Early feedback is welcome. If WinAirBar helps your workflow, opening an issue with your Windows version, use case, and what felt confusing is genuinely useful.

Good first feedback areas:

- Trigger behavior.
- Window switching ergonomics.
- App launcher behavior.
- Theme polish.
- Release/install friction.
- Missing system actions.

## License

WinAirBar is released under the MIT License. See `LICENSE`.
