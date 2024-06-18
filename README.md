# FontRegister

FontRegister is a small Windows utility to install fonts and/or repair the font registry via command line. This library wraps [FontReg](http://code.kliu.org/misc/fontreg/) with a more flowing API.

## Supported Font Types

- `fon`
- `ttf`
- `ttc`
- `otf`

## Usage

```sh
# Will start a command line interface:
fontregister

# View all available commands:
fontregister --help

# Perform registry cleanup and font repair:
fontregister --cleanup
fontregister --clear

# Register fonts in the following folders or specific files:
# Note: Folders are deep-searched recursively.
fontregister "c:/folder" "c:/font.ttf" "./relativedir/" "./relativedir/font.otf"
```

## Help

Here's the output of the help command:

```sh
--cleanup / --clear:
    Will remove any stale font registrations in the registry.
    Will repair any missing font registrations for fonts located in the C:\Windows\Fonts directory (this step
    will be skipped for .fon fonts if FontReg cannot determine which
    fonts should have "hidden" registrations).

"c:/path1" "c:/font.ttf" ... "./relativedir/":
    Will add or replace a font from the given path/folder.
    Note: Folders are deep-searched recursively.
```

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgements

- [FontReg](http://code.kliu.org/misc/fontreg/) for the underlying functionality.
- All contributors and users for their support.
