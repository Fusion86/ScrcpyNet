# ScrcpyNet

A work in progress reimplementation of the [scrcpy client](https://github.com/Genymobile/scrcpy/tree/master/app) in C#.

## Manually start the ADB Server

Before starting the program you need to manually start the ADB daemon.  
You can do this by running `adb devices` in your terminal.  
Make sure to also authorize the device by clicking "Accept" (or whatever) on your device.

![image](https://user-images.githubusercontent.com/4460428/114620342-c5fd8900-9cab-11eb-9078-38d1bcba405d.png)  
_This screenshot was taken before I clicked "Accept" on my phone, make sure to do that!_

## Setup

You'll need to manually install de ffmpeg binaries and the `scrpy-server.jar` file.  
These files should be in the same folder as the `ScrcpyNet.Avalonia.Sample.exe` file.

### FFMPEG Libraries

You can download the ffmpeg binaries from [here](https://www.gyan.dev/ffmpeg/builds/).  
You need to download the `ffmpeg-release-full-shared.7z` file.  
Extract all files .dll from the `bin` folder in the application root.

### scrcpy-server.jar

You'll need to download the `scrcpy-server-v1.17` file from [here](https://github.com/Genymobile/scrcpy/releases/tag/v1.17).  
Rename this file to `scrcpy-server.jar` and place it in the application root.

Example:  
_I couldn't fit all .dlls in this screenshot (e.g. swscale-5.dll), but you also need those.  
So make sure to extract all `*.dll` files from the `bin` directory inside `ffmpeg-release-full-shared.7z`_
![image](https://user-images.githubusercontent.com/4460428/114621567-24773700-9cad-11eb-811c-ad48a352e9ab.png)

## Notes

This code (ab)uses `unsafe` code inside C#.

The Avalonia frontend is quite crappy, but I believe this is also because of some bugs inside Avalonia (frames don't feel 'smooth' and sometimes it crashes).

If you set the bitrate too high the videodecoder might not be able to keep up and lag. Or you'll get an timeout error which crashes the program.

## Credits

- [Genymobile/scrcpy](https://github.com/Genymobile/scrcpy) - this code is based on their client implementation, and we use their scrcpy-server.jar
- [The Android Open Source Project](https://android.googlesource.com/platform/frameworks/native/+/master/include/android) - for the input/keycodes
