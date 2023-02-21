# Dotnet SDK Version Manager

**(WORK IN PROGRESS)**

A multi-platform CLI for managing .NET SDK installations, inspired by [Dots](https://github.com/nor0x/dots)

## Features

- list installed and available SDKs
- easily upgrade to the latest SDK
- install and uninstall specific SDK versions
- get detailed info about a SDK (WIP)
- support for MacOS and Linux (Windows TBD)

## TODO

- Prettier output (perhaps Spectre)
- Logging
- Better exception handling
- Tests

## Examples

```shell
# List installed SDKs
dvm list

# upgrade to the latest SDK available for all installed frameworks
sudo dvm upgrade

# upgrade to the latest SDK available for the .NET 6.0 framework
sudo dvm upgrade -f net6.0

# List all available SDKs for the .NET 7.0 framework
dvm list-available -f net7.0

# install a specific SDK version
sudo dvm install 7.0.200

# uninstall a specific SDK version
sudo dvm uninstall 7.0.102
```