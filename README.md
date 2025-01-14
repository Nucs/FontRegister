# <img src="https://i.imgur.com/Q4WoRjy.png" width="25" style="margin: 5px 0px 0px 10px"/> FontRegister
[![Nuget downloads](https://img.shields.io/nuget/vpre/FontRegister.svg)](https://www.nuget.org/packages/FontRegister/)
[![NuGet](https://img.shields.io/nuget/dt/FontRegister.svg)](https://github.com/Nucs/FontRegister)
[![GitHub license](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/Nucs/FontRegister/blob/master/LICENSE)

FontRegister is both a command-line tool and a csharp native library (pure code) for installing and uninstalling fonts on Windows.

## Key Features
- Ability to install fonts in bulk.
- Notify Windows OS about new fonts refreshing the font-lists on other apps immediately (photoshop, word and so on).
- Written in pure C# and Pinvoke.
- Code install/uninstall API via Nuget supporting .NET 4.8 and .NET 6.0.

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
fontregister install --user "c:/folder" "c:/font.ttf"
fontregister install -u "c:/folder" "c:/font.ttf"

# For all users (requires admin rights)
fontregister install --machine "c:/folder" "c:/font.ttf"
fontregister install -m "c:/folder" "c:/font.ttf"
fontregister install --all-users "c:/folder" "c:/font.ttf"

# 3. External Font Installation
# Registers fonts without copying them (keeps original location)
fontregister install --external "c:/folder/myfont.ttf"
fontregister install --external "c:/fonts-folder"

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

# 3. Uninstall with Scope
fontregister uninstall -m "fontname"  # Machine-wide
fontregister uninstall -u "fontname"  # User scope
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
  --machine, -m     : Install for all users
  --all-users       : Same as --machine
  --external        : Install fonts by referencing their original location
                      instead of copying them to the Windows Fonts directory
Note: All font operations require administrator rights
```

## FontRegister Library Code Example

```sh
PM> Install-Package FontRegister
```

```csharp
//Note: All font operations require administrator rights

//single file for current user
var notifier = new WindowsSystemNotifier(); //pass null to not notify other apps
var userInstaller = new WindowsUserFontInstaller(notifier);
var fontManager = new FontManager(userInstaller);
// Install by copying to Windows Fonts directory (default)
fontManager.InstallFonts(new[] { "C:/myfonts/myfont.ttf" }, false);
// Install by referencing original location
fontManager.InstallFonts(new[] { "C:/myfonts/myfont.ttf" }, true);

//in bulk for all users
var machineInstaller = new WindowsMachineFontInstaller(notifier);
var fontManager = new FontManager(machineInstaller);
fontManager.InstallFonts(new[] { "C:/myfonts", "C:/myfonts2/myfont.ttf" });
```

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgements

- All contributors and users for their support.
