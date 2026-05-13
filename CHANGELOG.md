# Changelog

## WinAirBar v1.4.0 - Workspace Control Center

WinAirBar v1.4.0 turns Workspace Memory into a full local Workspace Control
Center with visual workspace cards, restore previews, safer cleanup choices,
versions, timeline history, rules, suggestions, launcher tags, and stronger
backup import handling.

### What's New

- Added the dedicated Workspace Control Center with workspace cards, details,
  restore planning, screen galleries, metadata, rules, suggestions, launcher
  tags, groups, and timeline views.
- Added preview-first restore planning with matched windows, launch targets,
  missing items, low-confidence matches, monitor remaps, changed positions, and
  extra current windows.
- Added restore modes for Full Restore, Missing Only, Layout Only, and Clean
  Restore.
- Added Clean Restore cleanup choices for extra windows, with protected/risky
  windows kept by default and close actions requiring final confirmation.
- Added workspace versions before updates, capped at the latest 10 versions per
  workspace.
- Added detailed local timeline logging for workspace changes, restore results,
  cleanup actions, backup import/export, and suggestion decisions.
- Added the Workspace Switcher overlay and bottom-action path for fast visual
  workspace switching.
- Added deterministic local workspace suggestions based on repeated app/window
  groups, with save, dismiss, and never-suggest actions.
- Added Window Rules v1 for capture exclusions, cleanup defaults, restore mode
  defaults, and default placement.
- Added pinned window groups, multiple launcher tags per app, and tag editing.
- Added per-workspace auto-actions for apps, files, folders, URLs, settings
  URIs, theme/accent changes, volume/mute, and always-confirmed power/session
  actions.
- Added backup conflict previews with per-conflict choices: Keep Local, Import
  As Copy, or Overwrite.
- Added screen memory for workspaces, including larger galleries and thumbnail
  previews in the workspace list.
- Added `Force Close` to window right-click actions.

### UI Polish

- Kept the quick Workspace flyout light while moving deeper controls into the
  Control Center.
- Added a dedicated header button for quick workspace capture and kept the full
  Workspace Control Center available from the workspace button/bottom action.
- Improved dark-theme contrast across Control Center tabs, cards, and controls.
- Added right-click workspace card actions for preview, restore, update,
  rename, duplicate, copy name, snapshot, switcher, and delete.
- Made full secondary windows close the small launcher menu quickly after they
  open.
- Added long-name hover tooltips and responsive workspace card sizing.

### Upgrade Notes

Existing workspace JSON remains compatible. New v1.4 fields are optional and
stay readable under `%AppData%\WinAirBar`.

Workspace screenshots are stored locally under `%AppData%\WinAirBar` and are
captured when saving or updating a workspace.

Clean Restore never closes extra windows by default. Closing windows requires a
preview and a final confirmation.

### Download

Use the Windows x64 self-contained EXE:

`WinAirBar-v1.4.0-win-x64.exe`

SHA256:

`B9F7F7EFF024893F5071EA72EDE68EA41C0FBC46463056CE5F6E50FB3CA7FC0B`

Verify in PowerShell:

```powershell
Get-FileHash .\WinAirBar-v1.4.0-win-x64.exe -Algorithm SHA256
```

### Technical Notes

- Version bumped to `1.4.0`.
- Application manifest bumped to `1.4.0.0`.
- Release remains local-first, telemetry-free, account-free, and MIT/free.
- `WorkspaceAudit` coverage passed `31/31` during release-candidate validation.
- No installer is required.
- The executable is unsigned, so Windows SmartScreen may show a first-run
  warning.

## WinAirBar v1.3.0 - Workspace Memory And Windows 11 Polish

WinAirBar v1.3.0 adds Workspace Memory, improves restore confidence before
you act, and polishes the default UI into a cleaner Windows 11-style release.

### What's New

- Added **Workspace Memory** for capturing, restoring, updating, and deleting
  saved window layouts from the WinAirBar menu.
- Added preflight workspace clarity: saved window counts, monitor layout
  differences, missing apps/documents, and restore-readiness labels are visible
  before restore.
- Added smarter restore behavior for existing windows, monitor remaps, missing
  targets, and document-aware launches.
- Added inline restore result feedback showing restored, launched, skipped, and
  failed counts without forcing users into a large diagnostics view.
- Added focused `WorkspaceAudit` coverage for layout remapping, match scoring,
  document target detection, picker formatting, and restore-result counts.

### UI Polish

- Reworked the default Dark/Light styling toward a cleaner Windows 11 menu feel:
  sharper corners, quieter surfaces, subtler shadows, and neutral command
  buttons.
- Fixed Settings dropdown contrast by replacing system ComboBox popup colors
  with app-themed dropdown styles.
- Replaced WinAirBar-owned system message boxes with app-themed confirmation
  and info dialogs.
- Improved capture/confirmation dialog button sizing so OK/Yes/No targets are
  easier to click and no longer clipped.
- Added consistent hover tooltips across core controls, workspace controls,
  settings, window rows, tray controls, and dialogs.

### Upgrade Notes

Existing WinAirBar settings remain local under `%AppData%\WinAirBar`.
Existing users on the old default 12px radius are migrated once to the new
8px default for the modern Dark/Light themes; manual corner-radius choices can
still be adjusted in Settings.

### Download

Use the Windows x64 self-contained EXE:

`WinAirBar-v1.3.0-win-x64.exe`

SHA256:

`2915BE21BCF8DC6E870649BCD1F5E8EAFB2B5D6BC43BD4FEA61EF80C0A6C9A59`

Verify in PowerShell:

```powershell
Get-FileHash .\WinAirBar-v1.3.0-win-x64.exe -Algorithm SHA256
```

### Technical Notes

- Version bumped to `1.3.0`.
- Application manifest bumped to `1.3.0.0`.
- Restore behavior and workspace JSON compatibility stay local-first.
- Release package includes the EXE, release README, and SHA256 checksum.

## WinAirBar v1.2.0 - New Name, New Home

AirBar is now **WinAirBar**.

This release updates the app's public identity, metadata, website, support
contact, release package names, and Windows-facing branding while keeping the
same lightweight floating taskbar workflow.

### What's New

- Renamed the product from **AirBar** to **WinAirBar**.
- Added the official website: https://winairbar.com
- Added the support/security contact: sag@winairbar.com
- Updated the app title, tray text, splash screen, settings windows, README,
  security policy, license metadata, and package metadata.
- Updated the Windows executable and release package naming for v1.2.0.
- Preserved existing user settings with a safe migration from `%AppData%\AirBar`
  to `%AppData%\WinAirBar`.

### Upgrade Notes

Existing AirBar users should keep their settings, pinned launcher data, profiles,
and window history after launching WinAirBar v1.2.0.

The old `%AppData%\AirBar` folder is left in place as a backup. WinAirBar now
uses `%AppData%\WinAirBar`.

### Download

Use the Windows x64 self-contained EXE:

`WinAirBar-v1.2.0-win-x64.exe`

SHA256:

`0F8175C93B8CAAE4F3F5E5C2AB2FA6342BE1534871D25A09799273E6C986FA21`

Verify in PowerShell:

```powershell
Get-FileHash .\WinAirBar-v1.2.0-win-x64.exe -Algorithm SHA256
```

### Technical Notes

- Version bumped to `1.2.0`.
- Assembly/output name changed to `WinAirBar`.
- Application metadata now points to `https://winairbar.com`.
- Contact and security reporting now use `sag@winairbar.com`.
- Autostart registry naming is migrated from `AirBar` to `WinAirBar` when applicable.
- Internal implementation namespaces are intentionally left stable to avoid unnecessary churn.

