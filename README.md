# ConTeXt IDE
This is an IDE for the ConTeXt/LuaMetaTeX typesetting system. It is written in C# 9 / .NET 6 with the Windows App SDK 1.6 framework using only controls from the UI library WinUI 3. In the future, it *might* be possible to turn this into a .NET MAUI app that runs cross-platform on Windows, MacOS and (highly unlikely) also on Linux.

![Screenshot_App](https://raw.githubusercontent.com/WelterDevelopment/ConTeXt-IDE-WinUI/main/Scr_Editor.png)

## Installation
The app can be installed via the Microsoft store on x64/x86 devices with Windows 10 version 17763 and up: <a href='https://www.microsoft.com/store/apps/9NN9Q389TTJR?cid=storebadge&ocid=badge'><img src='https://developer.microsoft.com/store/badges/images/English_get_L.png' alt='English badge' height=20 /></a>

## Testing & Contribution

- pull https://github.com/WelterDevelopment/PDFjs.WinUI in Visual Studio (e.g. to c:\ConTeXtIDE\PDFjs.WinUI)
- pull https://github.com/WelterDevelopment/CodeEditorContol-WinUI in Visual Studio (e.g. to c:\ConTeXtIDE\CodeEditorContol-WinUI)
- pull this repo (https://github.com/WelterDevelopment/ConTeXt-IDE-WinUI) in Visual Studio (e.g. to c:\ConTeXtIDE\ConTeXt-IDE-WinUI)
- now that all 3 projects are in the same root folder, you should be able to open the solution file C:\ConTeXtIDE\ConTeXt-IDE-WinUI\ConTeXt-IDE-WinUI.sln
- if you see errors in the solution explorer, add the corresponding .csproj files manually

## Known bugs and missing features
Bugs and Limitations:
- No ARM support (I dont know why the app crashes on startup)
- No app lifecycle methods (open files from the Windows Explorer)
- No windowing (The pdf output cannot be undocked from the app)
- No line wrapping
- No code folding

## Changelog

### 2.1.1 (2022-06-26)
- Update to Windows App SDK 1.1.1
- 2 additional styles ("Materials"): Mica and Acrylic
- Compiler and installation processes now show a progress bar in the taskbar icon
- The deviders now look more appealing

### 2.0.0 (2022-05-08)
- Added a markdown previewer
- Added Markdown and CV (inspired from AltaCV) templates
- You can now use an external pdf viewer (like SumatraPDF)
- Adaptation of the new Microsoft Office design (margin and rounded corners for the ribbon)
- The ribbon is now collapsible

### 1.11.6 (2022-01-29)
- The app now listens to file system changes, meaning that you can add, remove, rename and edit files outside of the app while the project is open
- Multi-instancing is now working, so you can open different ConTeXt projects in seperate windows at once
- The jump list (= context menu of the task bar icon) entries now actually work and open a new instance with the corresponding project
- Bug fixes

### 1.10.45 (2022-01-16)
- In Windows 11, the Ribbon now extends into the title bar to save space
- The command reference got visually polished
- Drag and drop a command reference entry to your editor to quickly insert the command
- Mark commands as favorite, filter the command reference for your favorites
- The version of your ConTeXt distribution is now displayed
- The "new line" control character is now displayed correctly
- Fixed a bug where the app would not shutdown completely

### 1.10.40 (2022-01-09)
- Two-way SyncTeX is now implemented: Double-click a word in the PDF viewer to jump to the corresponding position in the source file
- Minor bug fixes

### 1.10.38 (2022-01-06)
- One-way SyncTeX is now implemented: Press Ctrl+Space to jump to the corresponding position in the pdf
- Fixed some bugs with IntelliSense
- PDF.js viewer got further optimized to integrate into the UI

### 1.10.25 (2021-12-31)
- The min/max/close buttons returned in Windows 10
- The syntax highlighting colors are now freely adjustable with direct visual impact on the editor
- IntelliSense and the command reference have been improved
- Theme and accent color changes now have smooth transitions
- Viewer has been updated to PDF.js version 2.12.313
- Viewer has been tweaked to update on theme changes
- If an already compiled PDF is in the project folder it gets opened automatically
- A status bar shows when the ConTeXt distribution is getting updated

### 1.10.5 (2021-11-29)
- Serious design improvements using the new Windows 11 styled controls.
- DropDownButtons are now easier distinguishable from Buttons and ToggleButtons
- Theme and accent color are changeable antime without bugs
- The use of the accent color is much more sensible now
- Important bug fixes

### 1.10.4 (2021-11-28)
- File outline: you always see in which section of your document you are working in.
- Project dropdown instead of side panel: remove all the clutter so you can concentrate on just your tex file(s) and the pdf output.
- Improved application log: You can now also see the console output from the context.exe compiler.
- When you close the app and have unsaved opened files, there will be a prompt that asks you whether you want to save these files.

### 2021-06-01 (Version 1.5.0)
- You can now install and update ConTeXt modules directly from the source (CTAN or ConTeXtGarden)
- Added a Setting for the editor's font size
- Bug fixes: The "Toggle pin" functionality does not cause app crashes anymore (but is still quite janky); Parsing errors of the *-error.log file have been resolved

### 2021-05-23 (Version 1.4.7)
- Saving, compiling and other file operations are no longer blocking the UI thread. This increases the "smoothness" of the app tremendously.
- Instead of being forced to the system accent color, you can now choose a different color from the palette. The accent color automatically gets darkened/lightened when the Dark/Light theme is applied. 
- Many improvements in the Light theme (whoever would want to use that ... :D), especially the syntax highlighting colors are a lot more readable now.
- Please keep in mind that changing the app's theme and accent color at runtime is a nightmare (even in Project Reunion ...). Bugs regarding theme and accent color changes are to be expected!

### 2021-05-05 (Version 1.3.44)
- I ported this app from UWP to the new Project Reunion 0.5 framework. This is an exciting first step towards a cross-platform ConTeXt IDE for Windows, MacOS and Linux with .NET MAUI (~Q4 2021)!