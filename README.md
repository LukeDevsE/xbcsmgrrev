Xbox Connected Storage Manager Revived
======================================
*A quickly written application to easily interact with Xbox Live game save data.*


### Preview
![Application Screenshot](assets/screenshot_example.png)

## Todo
- [x] Replace the login system with a code based system to be easier for the end user.
- [x] Make the folder (blob) name accurate
- [x] Save refresh token somewhere, so you dont need to keep logging in.
- [ ] Make better looking ui
- [ ] native linux support

## Requirements
- Windows (wine on linux might work too)
- an internet connection
- Any game that uses Xbox Live (*installed and played recently*)

## Known working games (as in games you can download save files from)
- UNDERTALE (possibly DELTARUNE)
- Minecraft: Xbox One Edition
- Slime Rancher
- HITMAN 3

Basically any singleplayer game

## Background
Accessing games using Xbox Live's save data requires authorization for the 'TitleStorage' service. This requires a user token. I created this application to make the process more clean and easier.

## Why?
I believe that everyone has the right to access their game save data on the games that they play. (if you no longer own a game, you can transfer your save files to your pc)

## Credits/References
- [XboxAuthNet](https://github.com/AlphaBs/XboxAuthNet)
- [Prism Launcher for OAuth](https://prismlauncher.org/)
- [Original xbcsmgr](https://github.com/billynothingelse/xbcsmgr)
