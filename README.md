# <img src="https://i.imgur.com/Q4WoRjy.png" width="25" style="margin: 5px 0px 0px 10px"/> FontRegister
[![Nuget downloads](https://img.shields.io/nuget/vpre/FontRegister.svg)](https://www.nuget.org/packages/FontRegister/)
[![NuGet](https://img.shields.io/nuget/dt/FontRegister.svg)](https://github.com/Nucs/FontRegister)
[![GitHub license](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/Nucs/FontRegister/blob/master/LICENSE)

FontRegister is both a command-line tool and a csharp native library (pure code) for installing and uninstalling fonts on Windows.

## Key Features
- Ability to install/uninstall fonts in bulk.
- Notify Windows OS about new fonts refreshing the font-lists on other apps immediately (photoshop, word and so on).
- Written in pure C# and Pinvoke with a simple code-first API.

## Supported Font Types

The following font file extensions are supported:
```csharp
".ttf"  // TrueType Font
".otf"  // OpenType Font
".fon"  // Windows Font
".ttc"  // TrueType Collection
".fnt"  // Windows Font
```

## Usage

```sh
# Note: All font operations require administrator rights

# INSTALLATION EXAMPLES:

# 1. Basic Installation (for current user)
# Installs fonts by copying them to Windows Fonts directory
fontregister install "c:/folder" "c:/font.ttf" "./relativedir/font.otf"
# Note: Folders are searched recursively for font files

# 2. Specify Installation Scope
# For current user (default)
fontregister install "c:/folder" "c:/font.ttf"
fontregister install --user "c:/folder" "c:/font.ttf"
fontregister install -u "c:/folder" "c:/font.ttf"

# For all users / machine-wide
fontregister install --machine "c:/folder" "c:/font.ttf"
fontregister install -m "c:/folder" "c:/font.ttf"
fontregister install --all-users "c:/folder" "c:/font.ttf"

# UNINSTALLATION EXAMPLES:

# 1. Basic Uninstallation
fontregister uninstall "fontname1" "fontname2"

# 2. Uninstall by Different Name Formats
# By font name as shown in Windows
fontregister uninstall "Calibri (TrueType)" "Calibri Light"
# By filename
fontregister uninstall "calibril.ttf"
# By full path
fontregister uninstall "C:/Windows/Fonts/calibril.ttf"
# By your installation path
fontregister uninstall "C:/folder/calibril.ttf"

# 3. Uninstall with Scope
fontregister uninstall -m "fontname"  # Machine-wide
fontregister uninstall -u "fontname"  # User scope

# UTILITIES EXAMPLES:

fontregister  # Display help
fontregister --clear-cache  # Clear font cache
```

## Help

Here's the output of the help command:

```sh
Usage: FontManager <command> [options] [paths...] 
Commands:
  install <path1> [path2] [path3] ... : Install fonts from specified files or directories
  uninstall <fontName1> [fontName2] [fontName3] ... : Uninstall specified fonts
Options:
  --user, -u        : Install for current user only (default)
  --machine, -m     : Install for all users (requires admin rights)
  --all-users       : Same as --machine
  --clear-cache, --restart-font-cache
                    : Restart the Windows Font Cache service after operation
                      refreshing font list and removing cached uninstalled fonts.
                      This command physically deletes %LOCALAPPDATA%\**\FontCache directories

Note: All font operations require administrator rights
```

## FontRegister Library Code Example

```sh
PM> Install-Package FontRegister
```

```csharp
using FontRegister;
using FontRegister.Abstraction;

// Note: All font operations require administrator rights

// Create system notifier to refresh font lists in other apps
var notifier = new WindowsSystemNotifier();

// Example 1: Install single font for current user
var userInstaller = new WindowsFontInstaller(notifier, InstallationScope.User);
var userFontManager = new FontManager(userInstaller);
userFontManager.InstallFonts(["C:/myfonts/myfont.ttf"]);

// Example 2: Install multiple fonts machine-wide
var machineInstaller = new WindowsFontInstaller(notifier, InstallationScope.Machine);
var machineFontManager = new FontManager(machineInstaller);
machineFontManager.InstallFonts([
    "C:/myfonts",          // Directory containing fonts
    "C:/myfonts2/myfont.ttf" // Single font file
]);

// Example 3: Uninstall fonts
machineFontManager.UninstallFonts([
    "MyFontName",          // By font name
    "myfont.ttf",          // By filename
    "C:/myfonts/myfont.ttf" // By full path
]);
```

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgements

- All contributors and users for their support.
