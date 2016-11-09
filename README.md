<img src="https://cloud.githubusercontent.com/assets/752380/20088997/43297c78-a584-11e6-9950-12741a2b14ec.png" align="right" width="128">

# dotCmd

> NOTE: This project s in heavy developement and it's curently in it's alpha stage. Everything is subject to change.

dotCmd aims to give .NET developers a much more advanced console by using the Native ConsoleHost APIs.


## Features

- [Independent output buffers] - dotConsole is capable of handling multiple output buffers that can be accessed using Regions. This enables users to create headers and footers that scroll with the main console output, as well as buffers that can scroll within the defined region of the console. Using regions it's easy to create controls like display tables or progress bars.

<p align="center">
  <img src="https://cloud.githubusercontent.com/assets/752380/20029600/64f61518-a350-11e6-8b8f-bdedff711d92.gif" align="center" width="512">
</p>

- [Full RGB Color handling] - dotConsole maps the standard console colors and creates full RGB color presets. These can be used only on a single region or the entire console buffer.

<p align="center">
  <img src="https://cloud.githubusercontent.com/assets/752380/20129990/002c9830-a654-11e6-94cb-f4426a1ad7c7.gif" align="center" width="512">
</p>

## Roadmap

- Tab completion and suggestion provider in input buffer.
- Input buffer histories.
- Command Line Argument parser and Console Generator by example.
- Composable API with much more Operations then a standard Console provides.
