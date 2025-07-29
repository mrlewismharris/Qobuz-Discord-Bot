# Qobuz Discord Bot
### Creating the Discord bot
From [Discord developer portal](https://discord.com/developers/applications) select your app, under the "Bot" menu item, toggle all intents ("Presence Intent", "Server Members Intent", "Message Content Intent").

To invite the bot to your Discord server, under "Installation" select method "Guild Install", then select the provider "Discord Provided Link", visit the link (whilst signed into Discord with server invite persmissions) and add the bot to your server.

## Installation
### Binaries
Required ffmpeg installed on system, install from the [official ffmpeg source](https://ffmpeg.org/download.html).

Download the package for your platform from the [Qobuz-Discord-Bot GitHub releases page](https://github.com/mrlewismharris/Qobuz-Discord-Bot/releases) and unzip (reminder on Linux use `tar -zxf filename.tar.gz`).

Modify the .env file with your credentials and options.

Run the executable from Windows or run from command line on Linux and MacOS.

### Linux
`apt install libopus0 libsodium23`

### Linux systemd setup (recommended)
Modify systemd service: `sudo nano /etc/systemd/system/qobuz-discord-bot.service` with:
```
[Unit]
Description=dotnet qobuz discord music bot
After=network.target

[Service]
WorkingDirectory={path-to-root-directory}
ExecStart={path-to-root-directory}/QobuzDiscordBot
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```
Useful systemctl commands:
```
sudo systemctl daemon-reload
sudo systemctl enable qobuz-discord-bot
sudo systemctl start qobuz-discord-bot
sudo systemctl status qobuz-discord-bot
```
The bot will now autorun on boot + crash, to disable use `sudo systemctl disable qobuz-discord-bot`.