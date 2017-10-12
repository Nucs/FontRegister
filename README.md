# FontRegister
FontRegisteris a small Windows utility to install fonts and/or repair the font registry via commandline.<br>
This library wraps http://code.kliu.org/misc/fontreg/ with a more flowing api.<br><br>
Supported font types: `fon`, `ttf`, `ttc`, `otf`.
## Usage
```C
//Will start a commandline interface:
fontregister
//View all available commands:
fontregister --help
//Perform registry cleanup and font repair:
fontregister --cleanup
fontregister --clear
//Register fonts in the following folders or specific file/s:
//Note: Folders are deep-searched recusively.
fontregister "c:/folder" "c:/font.ttf" "./relativedir/" "./relativedir/font.otf" 
```
        
## Help
Heres the output of help command:<br>
```CLI
--cleanup / --clear:
        Will remove any stale font registrations in the registry.
        Will repair any missing font registrations for fonts located in the C:\Windows\Fonts directory(this step
        will be skipped for .fon fonts if FontReg cannot determine which
        fonts should have "hidden" registrations).
"c:/path1" "c:/font.ttf" ... "./relativedir/"
        Will add or replace a font from given path/folder.
        Note: Folders are deep-searched recusively.
```
