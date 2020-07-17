# BridgeBotNext
![GitHub release (latest by date)](https://img.shields.io/github/v/release/maksimkurb/BridgeBotNext) ![Docker Pulls](https://img.shields.io/docker/pulls/maksimkurb/bridge-bot-next) ![build](https://github.com/maksimkurb/BridgeBotNext/workflows/build/badge.svg) ![release](https://github.com/maksimkurb/BridgeBotNext/workflows/release/badge.svg) ![GitHub stars](https://img.shields.io/github/stars/maksimkurb/BridgeBotNext?style=social)


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
You can deploy it to heroku with 1-click button:

[![Deploy](https://www.herokucdn.com/deploy/button.svg)](https://heroku.com/deploy?template=https://github.com/maksimkurb/BridgeBotNext)

### Required dependencies:
For Linux, make sure you have insalled libfontconfig:
```bash
apt-get install -y libfontconfig1
```


Set following env variables (aka Config Vars in Heroku):

|Key  |Sample value   | Description   |
|:---|----|----|
| BOT_VK__ACCESSTOKEN | abcdefg | Access token of VK bot  |
| BOT_VK__GROUPID | 1235467 | VK group id |
| BOT_TG__BOTTOKEN | 1234567:abcdefg | Access token of Telegram bot |
| BOT_AUTH__ENABLED | true  | Is bot settings protected with password (prevents 3rd parties from usage of your bot instance) |
| BOT_AUTH__PASSWORD | pa$$w0rd | Bot password, if enabled |
