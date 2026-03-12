# Input & Output Switcher

Tiny Windows 11 desktop utility for instantly switching the default audio output and microphone.

## Features

- Switch output devices as soon as you pick one from the dropdown.
- Switch input devices as soon as you pick one from the dropdown.
- Set all Windows audio roles at once for the chosen device.
- Keep the window pinned above other windows with `Pin on top`.
- Refresh the device list on demand.

## Build

```powershell
dotnet build .\InputOutputSwitcher.csproj
```

## Publish a lightweight EXE

```powershell
dotnet publish .\InputOutputSwitcher.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false
```

The published executable will be placed under:

`bin\Release\net8.0-windows\win-x64\publish\InputOutputSwitcher.exe`
