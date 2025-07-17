# GPSD Location Plugin

Intended for telescope hosting locations with many telescopes on site, to reduce configuration time and eliminate troubleshooting steps.

Or other nerds who have a GPS receiver in their observatories.

## Building

The plugin targets **.NET 8.0** for Windows. To build locally run:

```bash
dotnet restore
dotnet build -c Release
```

## Continuous Integration

GitHub Actions builds the plugin on every push and pull request. The workflow produces two artifacts:

* `plugin.zip` – the compiled plugin DLL ready for installation
* `package` – a NuGet package of the plugin

Artifacts can be downloaded from each workflow run or the releases page.

## Installing

Download `plugin.zip` from a release and extract the DLL into your N.I.N.A. plugins folder (typically `%LOCALAPPDATA%\NINA\Plugins\3.0.0\GPSDLocationPlugin`).
