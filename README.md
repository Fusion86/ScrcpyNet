# ScrcpyNet

A work in progress reimplementation of the [scrcpy client](https://github.com/Genymobile/scrcpy/tree/master/app) in C#.

## Features

- Basic keyboard input support
- Basic touch and swiping
- Automatic screen rotation
- Window resizing
- **No** audio support
- _Usually_ crash free

## Screenshot

![Screenshot](https://i.imgur.com/yGTl9Vy.png)

## Manually start the ADB Server

Before starting the program you need to manually start the ADB daemon.  
You can do this by running `adb devices` in your terminal.  
Make sure to also authorize the device by clicking "Accept" (or whatever) on your device.

![Example](https://user-images.githubusercontent.com/4460428/114620342-c5fd8900-9cab-11eb-9078-38d1bcba405d.png)  
_This screenshot was taken before I clicked "Accept" on my phone, make sure to do that!_

## Setup

The ScrcpyNet library should automatically copy the files from the deps/{shared,win64} folder to the ScrcpyNet folder inside your bin folder.
If for some reason this doesn't happen then you can manually copy those files to a ScrcpyNet folder next to your executable. 

## Notes

This code (ab)uses `unsafe` code inside C#.

The Avalonia frontend is quite crappy, but I believe this is also because of some bugs inside Avalonia (frames don't feel 'smooth' and sometimes it crashes).

If you set the bitrate too high the videodecoder might not be able to keep up and lag. Or you'll get an timeout error which crashes the program.

## Credits

- [Genymobile/scrcpy](https://github.com/Genymobile/scrcpy) - this code is based on their client implementation, and we use their scrcpy-server.jar
- [The Android Open Source Project](https://android.googlesource.com/platform/frameworks/native/+/master/include/android) - for the input/keycodes
