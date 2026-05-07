# AirBar

<div align="center">

![AirBar Logo](https://img.shields.io/badge/AirBar-Lightweight%20Command%20Bar-blue?style=for-the-badge)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)
[![Language](https://img.shields.io/badge/Language-C%23-purple?style=flat-square)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D4?style=flat-square)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat-square)](https://dotnet.microsoft.com)

*A fast, lightweight floating command bar for Windows that brings essential system controls and app management into one elegant, responsive flyout.*

</div>

---

## 📋 Overview

AirBar is a modern productivity tool designed for Windows users who demand efficiency without bloat. It provides instant access to window management, app launching, system controls, and customization—all delivered through a sleek, locally-powered interface with zero external dependencies.

### Why AirBar?

- **⚡ Blazing Fast** – Optimized C# implementation for instant responsiveness
- **🔒 Privacy-First** – 100% local operation, no cloud connectivity or telemetry
- **🎨 Themeable** – Classic Windows-inspired themes with full customization
- **📦 Lightweight** – Minimal memory footprint and resource usage
- **🔌 Self-Contained** – No external dependencies or complex setup

---

## ✨ Features

### Window Management
- **Quick Window Switcher** – Seamlessly switch between open windows with preview thumbnails
- **Recent History** – Fast access to recently used applications and documents
- **Window Search** – Intelligent search to find and launch any open window
- **Long-press Mouse Trigger** – Ergonomic activation method

### Application & Shortcuts
- **Pinned Apps** – Quick-launch your favorite applications with one click
- **Frequent Apps** – Automatically learns and suggests your most-used programs
- **Custom Shortcuts** – Create personalized launcher shortcuts for frequently used commands
- **App Groups** – Organize shortcuts into logical categories for better accessibility

### System Controls
- **Volume Control** – Adjust system volume directly from the flyout
- **Power Options** – Quick access to sleep, restart, shutdown, and hibernation with confirmation dialogs
- **Wi-Fi Management** – View and switch between available networks instantly
- **System Tray Integration** – Minimize to tray for unobtrusive background operation
- **Configurable Action Bar** – Customize bottom action bar with your most-used controls

### Customization
- **Multiple Themes** – Dark, light, and retro Windows-style flyouts and context menus
- **Appearance Settings** – Customize colors, fonts, transparency, and layout
- **Profile Management** – Save and switch between different configurations
- **User Preferences** – Persistent settings stored locally for seamless experience across sessions

---

## 🚀 Getting Started

### System Requirements

- **OS:** Windows 10 or Windows 11
- **.NET Runtime:** .NET 8.0 or higher
- **RAM:** Minimum 50MB
- **Disk Space:** ~20MB for installation

### Installation

1. **Download the latest release** from [Releases](https://github.com/UncostlyDevs/AirBar/releases)
2. **Extract the archive** to your preferred location (or run the installer)
3. **Run `AirBar.exe`** to launch the application
4. **Configure settings** via the tray menu (optional)

### Quick Start

- **Launch AirBar:** Double-click `AirBar.exe` or create a shortcut
- **Open Flyout:** Use the configured mouse trigger or keyboard hotkey
- **Access Features:** Navigate through the menu using keyboard or mouse
- **Customize:** Right-click the system tray icon → Settings

---

## ⚙️ Configuration

### Storage & Settings

All AirBar settings are stored locally in your user profile:
- **Location:** `%AppData%\AirBar`
- **Contents:** Settings, launcher data, pinned profiles, logs, and window history
- **No Cloud Sync:** All data remains on your machine

### Configurable Options

- **Mouse Trigger** – Long-press mouse button to open the floating menu (customizable)
- **Bottom Action Bar** – Customize quick-access buttons for power, volume, Wi-Fi
- **Theme Selection** – Choose between dark, light, or retro Windows styles
- **Autostart** – Automatically launch AirBar on Windows startup
- **Custom Actions** – Define shortcuts to applications, commands, or scripts

---

## 🏗️ Architecture

### Core Components

```
AirBar/
├── App.xaml / App.xaml.cs     # Application startup, theme loading, tray icon
├── Controls/                    # UI components
│   ├── WindowListControl.xaml   # Window switcher interface
│   ├── LauncherControl.xaml     # App launcher interface
│   ├── SettingsControl.xaml     # Settings panel
│   └── BottomActionsBar.xaml    # Configurable action bar
├── Core/                        # Core functionality engine
│   ├── WindowTracker.cs         # Window enumeration & management
│   ├── AppLauncher.cs           # Application launching logic
│   ├── HistoryManager.cs        # Recent history tracking
│   ├── SystemControls.cs        # Power, volume, Wi-Fi controls
│   ├── ThemeManager.cs          # Theme & styling engine
│   └── SettingsService.cs       # Configuration management
├── Models/                      # Data models & structures
│   ├── AppEntry.cs              # Application model
│   ├── WindowInfo.cs            # Window information model
│   ├── AppSettings.cs           # Settings model
│   └── ThemeConfig.cs           # Theme configuration model
├── Windows/                     # Window definitions
│   ├── FlyoutWindow.xaml        # Main floating flyout window
│   └── SettingsWindow.xaml      # Settings dialog
├── Styles/                      # Shared UI resources
│   └── Windows11Styles.xaml     # Windows 11 style resources
└── Assets/                      # Application assets
    ├── AirBar.ico               # Application icon
    └── Logo.png                 # Logo image
```

### Technology Stack

- **Language:** C# (.NET 8.0+)
- **UI Framework:** WPF (Windows Presentation Foundation)
- **Interop:** P/Invoke for native Windows API calls
- **Configuration:** JSON-based settings serialization
- **Packaging:** Self-contained executable (no installer required)

---

## 📊 Performance

- **Memory Usage:** ~40-80MB at runtime
- **CPU Impact:** <1% during idle, minimal spikes during operations
- **Startup Time:** <500ms on modern hardware
- **Flyout Response:** <100ms from activation to display

---

## 🛠️ Development

### Prerequisites

- Visual Studio 2022 (or VS Code + .NET CLI)
- .NET 8.0 SDK or higher
- Windows 10/11 development environment

### Building from Source

```powershell
# Clone the repository
git clone https://github.com/UncostlyDevs/AirBar.git
cd AirBar

# Restore dependencies
dotnet restore FloatingTaskbarMenu.csproj

# Build the project
dotnet build FloatingTaskbarMenu.csproj -c Release

# Run
dotnet run --project FloatingTaskbarMenu.csproj
```

### Publish A Windows Build

```powershell
dotnet publish FloatingTaskbarMenu.csproj -c Release -r win-x64 --self-contained true
```

The self-contained executable is written to `bin\Release\net8.0-windows\win-x64\publish\AirBar.exe`.

### Verify Build Integrity

```powershell
Get-FileHash .\bin\Release\net8.0-windows\win-x64\publish\AirBar.exe -Algorithm SHA256
```

### Contributing

We welcome contributions! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 🐛 Troubleshooting

### AirBar Won't Launch
- Ensure .NET 8.0+ is installed: `dotnet --version`
- Check that Windows Defender or your antivirus hasn't quarantined the executable
- Try running as Administrator
- Verify the executable hasn't been corrupted during download

### Mouse Trigger Not Working
- Verify no other application is capturing the same mouse trigger
- Restart AirBar to re-initialize input handling
- Check Settings to ensure the trigger is properly configured
- Try using a different mouse button

### High Memory Usage
- Close unused pinned apps in the launcher
- Clear recent window history in Settings
- Reduce theme complexity or disable transparency effects
- Check Task Manager for memory spikes during specific operations

### Crashes or Unexpected Behavior
- Check the log files in `%AppData%\AirBar\logs\`
- Verify you're running the latest version
- Try resetting settings to defaults
- [Open an issue](https://github.com/UncostlyDevs/AirBar/issues) with your log files and system information

---

## 🔒 Security & Privacy

- **No Telemetry** – AirBar does not collect or send app usage data to remote servers
- **Local Storage** – All settings, launcher data, pinned profiles, logs, and window history are stored locally under `%AppData%\AirBar`
- **No Admin Required** – AirBar runs with standard user privileges and does not require administrator access
- **Autostart Privacy** – Uses only the current user's Windows Run key for autostart
- **Confirmation Dialogs** – Power actions and first-time custom action launches ask for confirmation before execution
- **Download Verification** – Verify SHA256 checksums against published checksums with releases for added security

---

## 📝 License

This project is licensed under the MIT License – see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Inspired by classic Windows UI design principles
- Built with gratitude for the .NET and WPF communities
- Thanks to all contributors and early adopters

---

## 📮 Contact & Support

- **Issues & Bug Reports:** [GitHub Issues](https://github.com/UncostlyDevs/AirBar/issues)
- **Feature Requests:** [GitHub Discussions](https://github.com/UncostlyDevs/AirBar/discussions)
- **Documentation:** See README and troubleshooting section above

---

<div align="center">

Made with ❤️ by **UncostlyDevs**

⭐ If you find AirBar useful, please consider starring the repository!

</div>