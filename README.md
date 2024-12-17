# BridgeBotNext

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/maksimkurb/BridgeBotNext)](https://github.com/maksimkurb/BridgeBotNext/releases) [![Docker Pulls](https://img.shields.io/docker/pulls/maksimkurb/bridge-bot-next)](https://hub.docker.com/r/maksimkurb/bridge-bot-next) [![build](https://github.com/maksimkurb/BridgeBotNext/workflows/build/badge.svg)](https://github.com/maksimkurb/BridgeBotNext/actions?query=workflow%3Abuild) [![release](https://github.com/maksimkurb/BridgeBotNext/workflows/release/badge.svg)](https://github.com/maksimkurb/BridgeBotNext/actions?query=workflow%3Arelease) [![GitHub stars](https://img.shields.io/github/stars/maksimkurb/BridgeBotNext?style=social)](https://github.com/maksimkurb/BridgeBotNext/stargazers)

<p align="center">
	<img width="615" height="308" title="BridgeBotNext Logo" src="https://raw.githubusercontent.com/maksimkurb/BridgeBotNext/master/static/logo.png">
</p>

This is a new version of [MeBridgeBot](https://github.com/maksimkurb/MeBridgeBot), rewritten in C#

**Make a bridge between VK and Telegram conversations!**

Add @MeBridgeBot to your conversations in [Telegram](https://t.me/MeBridgeBot) and [VK](https://vk.com/mebridgebot), allow them to read your messages (in VK you should make it in conversation settings, you must be Administrator to do this)

To connect chats, enter `/token` command in first chat to get a special command with secret key.
Enter this special command in another chat (it looks like `/connect $mbb2$1!9d8xxxxx00ca`) and your chats are connected now!

## Screenshot

![Screenshot](https://raw.githubusercontent.com/maksimkurb/BridgeBotNext/master/static/screenshot.jpg)

## Deployment

### Docker Compose

You can run bot by creating docker container:

```bash
# download compose file
wget https://github.com/maksimkurb/BridgeBotNext/raw/refs/heads/master/compose.yaml

# then edit env values in compose.yaml file
nano compose.yaml

# and deploy
docker compose up -d
```

### Heroku

You can deploy bot to Heroku with 1-click button:

[![Deploy](https://www.herokucdn.com/deploy/button.svg)](https://heroku.com/deploy?template=https://github.com/maksimkurb/BridgeBotNext)

### Manual

For Linux, make sure you have insalled libfontconfig:

```bash
apt-get install -y libfontconfig1
```

Then, just download latest release, create appsettings.json, configure it and run `BridgeBotNext` executable.

## Configuration

### Environment

You can configure bot via the following environment variables:

|Key (notice double underscore)  |Sample value   | Description   |
|:---|----|----|
| BOT_VK__ACCESSTOKEN | abcdefg | Access token of VK bot  |
| BOT_VK__GROUPID | 1235467 | VK group id |
| BOT_TG__BOTTOKEN | 1234567:abcdefg | Access token of Telegram bot |
| BOT_AUTH__ENABLED | true  | Is bot settings protected with password (prevents 3rd parties from usage of your bot instance) |
| BOT_AUTH__PASSWORD | pa$$w0rd | Bot password, if enabled |
| BOT_DBPROVIDER | sqlite | Database provider (sqlite/postgres) |
| BOT_CONNECTIONSTRINGS_SQLITE | Data Source=./mydatabase.db | Path to the bot database |
| BOT_CONNECTIONSTRINGS_POSTGRES | Host=localhost;Database=postgres;Username=postgres;Password=postgres | Connection string for postgres |

### appsettings.json file

You can create `appsettings.json` configuration file, place it in the folder with BridgeBotNext:

```json
{
  "Vk": {
    "AccessToken": "abcdef",
    "GroupId": 170046687
  },
  "Tg": {
    "BotToken": "1234567:asdadasdasdasd"
  },
  "Auth": {
    "Enabled": true,
    "Password": "pa$$w0rd"
  },
  "DbProvider": "sqlite",
  "ConnectionStrings": {
    "sqlite": "Data Source=mydatabase.db",
    "postgres": "Host=localhost;Database=postgres;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "System": "Warning",
      "Microsoft": "Warning"
    },
    "Console": {
      "IncludeScopes": true
    }
  }
}
```
