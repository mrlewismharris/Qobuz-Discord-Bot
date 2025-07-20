# Qobuz Discord Bot
Qobuz Discord music bot built in dotnet.

Notes for creating a simple Discord bot, see bottom of readme.

# Requirements
> dotnet, ffmpeg, libopus.

## dotnet 9 sdk
Use the installer found [here](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

Windows: `winget install Microsoft.DotNet.SDK.9`
Linux:
```
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --channel 9.0
```

## ffmpeg
Use ffmpeg's recommended [cross-platform installers found here](https://ffmpeg.org/download.html).

## opus
### Windows
I'll include the compiled dlls but to update get dlls from: [https://github.com/DSharpPlus/DSharpPlus](https://github.com/DSharpPlus/DSharpPlus/raw/master/docs/natives/vnext_natives_win32_x64.zip), copy "libopus.dll" to root directory + rename/overwrite to "opus.dll".

### Linux
`apt install libopus0 libsodium23`

# Setup
Rename .env.example to .env and add your info.

### Notes for Discord Developer Portal
On the [Discord developer portal](https://discord.com/developers/applications), on your app, under the "Bot" menu item, toggle all intents ("Presence Intent", "Server Members Intent", "Message Content Intent").

Invite the bot, under "Installation" select method "Guild Install", then select the provider "Discord Provided Link", visit the link and add to your server.

Use `dotnet run` to start the bot, then summon the bot with `!play *query*` (replace with your prefix).