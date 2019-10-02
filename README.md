[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]



<!-- PROJECT LOGO -->
<br />
<p align="center">
  <a href="https://github.com/Davee02/DaHo.Chip8">
    <img src="res/img/logo.png" alt="Logo" width="80" height="80">
  </a>

  <h3 align="center">DaHo.Chip8</h3>

  <p align="center">
    A CHIP-8 emulator written in C#
    <br />
    ·
    <a href="https://github.com/Davee02/DaHo.Chip8/issues">Report Bug</a>
    ·
    <a href="https://github.com/Davee02/DaHo.Chip8/issues">Request Feature</a>
    ·
  </p>
</p>



## Table of Contents

* [About the Project](#about-the-project)
  * [Built With](#built-with)
* [Usage](#usage)
* [Contributing](#contributing)
* [License](#license)
* [Acknowledgements](#acknowledgements)



<!-- ABOUT THE PROJECT -->
## About The Project

![Running emulator screenshot][product-screenshot]

This is a CHIP-8 emulator written in C#. Because it's using the .NET-Core 3.0 framework, it can run on any OS.
The rendering of the sprites and the audio (a beep) is done with SFML.

### Built With
* [.NET Core](https://dotnet.microsoft.com/download/dotnet-core)
* [SFML](https://www.sfml-dev.org/tutorials/2.5/)


<!-- USAGE EXAMPLES -->
## Usage

To use the emulator, you have to specify the path to the ROM you want to use. The path is the first cli-argument you pass to the emulator.
Example usage:
`.\DaHo.Chip8.exe C:\emu\roms\TETRIS.ch8`

Here is a great list of ROMs: <https://github.com/stianeklund/chip8/tree/master/roms>

### Key Mapping

The original CHIP-8 had 16 virtual keys in the layout on the left, which has been mapped to the keyboard layout on the right:
```
 1 2 3 C                                   1 2 3 4
 4 5 6 D    This is emulated with these    Q W E R
 7 8 9 E    keyboard keys -->              A S D F
 A 0 B F                                   Y X C V
```

There are additional functions available:

| Key      | Description                                                      |
| -------- | ---------------------------------------------------------------- |
| `+`      | Increse emulation speed (max. 900 Hz)                            |
| `-`      | Decrease emulation speed (min. 10 Hz)                            |
| `PAUSE`  | Pause the emulator                                               |
| `DELETE` | Reset the emulator and begin to execute the ROM at the beginning |

<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to be learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## Additional Screenshots

<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE` for more information.


<!-- ACKNOWLEDGEMENTS -->
## Acknowledgements
* [SFML.Net](https://github.com/SFML/SFML.Net)
* [CHIP-8 emulator tutorial](http://www.multigesture.net/articles/how-to-write-an-emulator-chip-8-interpreter/)
* [EmuDev subreddit](https://www.reddit.com/r/EmuDev/)
* [CHIP-8 test ROM](https://github.com/corax89/chip8-test-rom)


<!-- MARKDOWN LINKS & IMAGES -->
[contributors-shield]: https://img.shields.io/github/contributors/Davee02/DaHo.Chip8.svg?style=flat-square
[contributors-url]: https://github.com/Davee02/DaHo.Chip8/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/Davee02/DaHo.Chip8.svg?style=flat-square
[forks-url]: https://github.com/Davee02/DaHo.Chip8/network/members
[stars-shield]: https://img.shields.io/github/stars/Davee02/DaHo.Chip8.svg?style=flat-square
[stars-url]: https://github.com/Davee02/DaHo.Chip8/stargazers
[issues-shield]: https://img.shields.io/github/issues/Davee02/DaHo.Chip8.svg?style=flat-square
[issues-url]: https://github.com/Davee02/DaHo.Chip8/issues
[license-shield]: https://img.shields.io/github/license/Davee02/DaHo.Chip8.svg?style=flat-square
[license-url]: https://github.com/Davee02/DaHo.Chip8/blob/master/LICENSE.txt
[product-screenshot]: images/screenshot.png
