# Qobuz Discord Bot
Qobuz Discord music bot built in dotnet.
> Early build. Bot can join, download and play a single track, then breaks.

# Requirements
> dotnet9, qobuz-dl (python), ffmpeg, libopus. All instruction see below.

## dotnet 9 sdk
Use the installer found [here](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

Windows: `winget install Microsoft.DotNet.SDK.9`
Linux:
```
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --channel 9.0
```

## Python3
Required for QobuzDL
[All platforms download from official website.](https://www.python.org/downloads/)

## qobuz-dl + qobuz account (Python required)
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

Run `qobuz-dl` in terminal/cmd/powershell to initialise and enter your Qobuz credentials.

**Note: qobuz-dl config settings will be overwritten by qobuz discord bot**

## ffmpeg
Use ffmpeg's recommended [cross-platform installers found here](https://ffmpeg.org/download.html).

## opus
### Windows
I'll include the compiled dlls but to update get dlls from: [https://github.com/DSharpPlus/DSharpPlus](https://github.com/DSharpPlus/DSharpPlus/raw/master/docs/natives/vnext_natives_win32_x64.zip), copy "libopus.dll" to root directory + rename/overwrite to "opus.dll".

### Linux
`apt install libopus0 libsodium23`

# Setup
Rename .env.example to .env and replace Discord token with your real discord bot token.

## Discord Bot
On Discord developer portal under "Bot" tab, toggle on all intents ("Presence Intent", "Server Members Intent", "Message Content Intent").

Invite the bot, under "Installation" select method "Guild Install", then select the provider "Discord Provided Link", visit the link and add to your server.

Use `dotnet run` to start the bot, then summon the bot with `!play *query*` (replace with your prefix).