# Hollow Knight Randomizer 3.0 Multiworld

NOTE: THIS README AND PROJECT ARE WIP

For general randomizer information, see: https://github.com/JasonILTG/HollowKnight.RandomizerMod

This mod is an extension of the existing Randomizer3.0 and MultiWorld mods to allow multiworld with all of the features of Randomizer3.0. A multiworld is a version of randomizer where items are not only scattered throughout your file, but through any number of linked game files. When picking up an item that belongs to another player, it is sent over the internet and they receive it in their game. This allows co-operative randomizer playthroughs, where you may need to pick up eachother's progression to go forward.

## Features
- All randomizer features from Randomizer3.0 are supported up to Geo Rocks, Lifeblood Cocoons, and Soul Totems, including area and room randomization.
- Per player settings - Each player has full access to all settings of the randomizer, meaning they make their own choice of which item pools to randomize, which skips are allowed in logic for their world, whether items/areas/rooms are randomized, and starting location
- Nicknames - Players can set a nickname for themselves which will show up when picking up their items in other worlds
- Support for saving and rejoining - Once a multiworld file is generated, quitting and rejoining should just work, no need to fuss with player IDs or server settings
- (Most) compatible with BingoUI - Counters may pop up at strange times, but they should be correct including items sent from other players

## Getting Started
1. Download the zip from the releases page on github: (TODO: LINK HERE)
2. Install SeanprCore.dll if you haven't already (this can be done through the mod installer: https://www.nexusmods.com/hollowknight/mods/9)
3. Copy MultiWorldProtocol.dll, RandomizerLib3.0.dll, and RandomizerMod3.0.dll into `Hollow Knight/hollow_knight_Data/Managed/Mods` (this will replace the existing RandomizerMod3.0 if you have it installed
4. Download MultiWorldServer.zip from releases and extract it to wherever you would like to run the server from
5. Port forward 38281 to the machine running the server (look up tutorials online for your router)
6. Run MultiWorldServer.exe

This is all that is needed in terms of setup. To play multiworld:

1. Open Hollow Knight and start a new file
2. Enter the IP address of the server, and set Multiworld to "Yes". This will connect to the server. If the connection fails, Multiworld will stay set to "No"
3. Enter the nickname you would like to use in the "Nickname" field
4. Configure your randomizer settings however you would like
5. Click "Ready" to toggle your ready status. The buttton will show how many players on the server are currently ready, and this will lock your settings in. If you would like to change settings, click again to become unready
6. Once everyone you are playing with is connected and readied up, one player should click start, and this will begin the randomizer for everyone. The player who clicks start will be marked as "Player 1", and their seed will be used for all randomization.

## Upcoming features
- Items flash in the bottom of the screen when receiving from other players
- Server commands, for example giving items to break hardlocks (unlikely, but can happen when Alt-F4ing)
