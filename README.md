# Acrylic4WPF

Acrylic4WPF is a remake of the [Acrylic Material](https://docs.microsoft.com/en-us/windows/uwp/design/style/acrylic) design from Microsoft that can only be used in UWP.
*This library is based on the work of [bbougot](https://github.com/bbougot/AcrylicWPF). All credits for creating transparency/blur effects go to him. 


## Download

Just download one of the releases here in GitHub or compile the project for yourself :)


## Usage

When creating a new window in WPF, instead of extending the ````Window```` class you should extend the ````AcrylWindow```` class. You need to to this both in XAML and in Code.


## Custimization

- You can change the acrylic opacity with the ````AcrylOpacity```` property
- Changing the acrylic colored background is possible with the ````TransparentBackground```` property
- Editing the blur noise is done by editing the ````NoiseRatio```` property
- Additionally you can enable/disable the TitleBar buttons with ````ShowMinimizeButton```` ````ShowFullscreen```` ````ShowCloseButton````


## Critical Information

- Do NOT change WindowStyle in your window, since the TitleBar is redesigned in this libary and changing causes crashes or two TitleBars
- Since WPF has a maximizing bug (maximizing the window larger than the screen actually is), when WindowStyle is set to None, this library uses aditional code the work around this bug. Still, it's not perfect, so display error can occur in rare cases. Mostly, when the user uses your app on two screens.
- 


## Further Information / ToDo

- Better window drag, when dragging out of maximized state
- 
