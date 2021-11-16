# ConTeXt IDE
This is an IDE for the ConTeXt/LuaMetaTeX typesetting system. It is written in C# 9 / .NET 5 with the Windows App SDK (Project Reunion) 0.8 framework using only controls from the UI library WinUI 3. In the future, it *might* be possible to turn this into a .NET MAUI app that runs cross-platform on Windows, MacOS and (highly unlikely) also on Linux.

![Screenshot 2021-11-14 191518](https://user-images.githubusercontent.com/13318246/141693303-d20e71cf-e98b-4f00-a83c-f454da1ea86c.png)

## Usage

### Installation
The app can be installed via the Microsoft store on x64 devices with Windows 10 version 17763 and up: <a href='https://www.microsoft.com/store/apps/9NN9Q389TTJR?cid=storebadge&ocid=badge'><img src='https://developer.microsoft.com/store/badges/images/English_get_L.png' alt='English badge' height=20 /></a>

### Known bugs and missing features
Bugs and Limitations that are directly caused by the current implementation of Project Reunion 0.8:
- No app lifecycle methods (open files from the Windows Explorer, check for unsaved files on app closing, ...)
- No windowing (The pdf output cannot be undocked from the app)
- Graphic glitches with the new WebView2 control (weird and random thin white borders at the top and left edges; suddenly occuring thick margins at the bottom and right edges)
- The ribbon is not part of the window's title bar. The Minimize/Maximize/Close buttons look different from the buttons the system is using.
- The context menu of the Application Log (RichTextBlock) shows white text when the dark theme is applied in the system settings and the white theme is applied in the app.

All these Issues will get fixed in ~Q4 2021

### Changelog
#### 2021-06-01 (Version 1.5.0)
- You can now install and update ConTeXt modules directly from the source (CTAN or ConTeXtGarden)
- Added a Setting for the editor's font size
- Bug fixes: The "Toggle pin" functionality does not cause app crashes anymore (but is still quite janky); Parsing errors of the *-error.log file have been resolved

#### 2021-05-23 (Version 1.4.7)
- Saving, compiling and other file operations are no longer blocking the UI thread. This increases the "smoothness" of the app tremendously.
- Instead of being forced to the system accent color, you can now choose a different color from the palette. The accent color automatically gets darkened/lightened when the Dark/Light theme is applied. 
- Many improvements in the Light theme (whoever would want to use that ... :D), especially the syntax highlighting colors are a lot more readable now.
- Please keep in mind that changing the app's theme and accent color at runtime is a nightmare (even in Project Reunion ...). Bugs regarding theme and accent color changes are to be expected!

#### 2021-05-05 (Version 1.3.44)
- I ported this app from UWP to the new Project Reunion 0.5 framework. This is an exciting first step towards a cross-platform ConTeXt IDE for Windows, MacOS and Linux with .NET MAUI (~Q4 2021)!

## Contributions
Pull requests are always appreciated. The IDE is pretty basic right now. More functions to suit more workflow types is always better. But please consider the app's "philosophy" before suggesting any changes!

### Philosophy - What is this app?
- Usability: This app provides the "Overleaf" experience for ConTeXt!
   - The UI should be self-explinatory.
   - Everything should just work "out-of-the-box".
   - No console needs to be used. If there are compiler errors they should get pretty-printed. Modes and other compiler parameters should be set graphically.
- Design guidelines: 
   - The UI should deeply integrate to the design of Windows 10 with its theming and accent colors.
   - The ribbon (basically a TabView) should be as self-explinatory as possible. It should not look overpopulated. Everything needs to be indicated by suitable icons.

### ToDo
If you want to contribute changes, here are some points that come to mind:

#### Functions
- Ability to download, install and update ConTeXt-compatible modules from CTAN.
- The in-app ConTeXt Templates should be fetched from a GitHub-Page at runtime.
- App localization: Provide basic language support for Spanish, French, Italian, German, ...
- Graphical implementation of every compiler parameter that is possible (for a parameter list see https://wiki.contextgarden.net/Context)

#### Code cleanup
- The PDF.JS viewer is currently located in ConTeXt-IDE.Shared (folders "Build" and "web"). Thats quite a mess and the viewer should be wraped in a WebView2-based WinUI3-control 
- The whole code looks quite messy (CodeMaid is not ready for Project Reunion and I'm too lazy to organize my classes...).
- Lots of zombie code and fiddly workarounds.
