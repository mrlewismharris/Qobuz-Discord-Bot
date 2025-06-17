# Qobuz Discord Bot

This is a Qobuz Music Discord Bot written using C#, dotnet 10.

# Requires
> dotnet 10, qobuz-dl (python)

## dotnet 10 sdk
This was written using 10.0.0-preview.5.25277.114

### Windows/MacOS:
Use the installer found here: [10.0.0-preview.5.25277.114](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

### Linux
### Install the dotnet SDK alongside existing SDK:
```
cd ~
wget https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-10.0.100-preview.5-linux-x64-binaries
mkdir -p /usr/share/dotnet && tar zxf dotnet-sdk-10.0.100-preview.5.25277.114-linux-x64.tar.gz -C /usr/share/dotnet
```
*Note, to export the dotnet path variable:*
```
export DOTNET_ROOT=/usr/share/dotnet
export PATH=$PATH:$HOME/dotnet
source ~/.bashrc
```

Verify it install with `dotnet --info`

## qobuz-dl + qobuz account
From [qobuz-dl GitHub repo](https://github.com/vitiko98/qobuz-dl):

### Linux / MAC OS
```
pip3 install --upgrade qobuz-dl
```

### Windows
```
pip3 install windows-curses
pip3 install --upgrade qobuz-dl
```

Run `qobuz-dl` in terminal/cmd/powershell and enter your Qobuz credentials.

**Note: qobuz-dl config settings will be overwritten by qobuz discord bot**

# Setup
Rename .env.example to .env and replace Discord token with your discord bot token.

dotnet 10 allows running .cs files directly, simply run: `dotnet run QobuzDiscordBot.cs`.

## ffmpeg
Use ffmpeg's recommended [cross-platform installers found here](https://ffmpeg.org/download.html).

## opus
### Windows
I'll include the compiled dlls but to update get libopus.dll from: [https://github.com/DSharpPlus/DSharpPlus](https://github.com/DSharpPlus/DSharpPlus/raw/master/docs/natives/vnext_natives_win32_x64.zip), copy "libopus.dll" to root directory + rename/overwrite to "opus.dll".

### Linux
`apt install libopus0 libsodium23`