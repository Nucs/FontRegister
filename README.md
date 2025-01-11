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
# Will print help
fontregister

# Register fonts for current user from the following folders or specific files (requires admin rights):
# Note: Folders are deep-searched recursively.
fontregister install "c:/folder" "c:/font.ttf" "./relativedir/" "./relativedir/font.otf"
# or explicitly
fontregister install --user "c:/folder" "c:/font.ttf"

# Register fonts for all users (requires admin rights):
fontregister install --machine "c:/folder" "c:/font.ttf"
# or
fontregister install -m "c:/folder" "c:/font.ttf"
# or
fontregister install --all-users "c:/folder" "c:/font.ttf"
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
fontManager.InstallFonts(new[] { "C:/myfonts/myfont.ttf" });

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

- [FontReg](http://code.kliu.org/misc/fontreg/) for the underlying functionality.
- All contributors and users for their support.
