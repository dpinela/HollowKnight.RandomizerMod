# Hollow Knight Randomizer 3.0 Multiworld

For general randomizer information, see: https://github.com/JasonILTG/HollowKnight.RandomizerMod

This mod is an extension of the existing Randomizer3.0 and MultiWorld mods to allow multiworld with all of the features of Randomizer3.0. A multiworld is a version of randomizer where items are not only scattered throughout your file, but through any number of linked game files. When picking up an item that belongs to another player, it is sent over the internet and they receive it in their game. This allows co-operative randomizer playthroughs, where you may need to pick up each other's progression to go forward.

## Features
- All randomizer features from Randomizer3.0 are supported up to Geo Rocks, Lifeblood Cocoons, and Soul Totems, including area and room randomization.
- Per player settings - Each player has full access to all settings of the randomizer, meaning they make their own choice of which item pools to randomize, which skips are allowed in logic for their world, whether items/areas/rooms are randomized, and starting location
- Nicknames - Players can set a nickname for themselves which will show up when picking up their items in other worlds
- Support for saving and rejoining - Once a multiworld file is generated, quitting and rejoining should just work, no need to fuss with player IDs or server settings
- (Mostly) compatible with BingoUI - Counters may pop up at strange times, but they should be correct including items sent from other players
- Support for room codes - When connected to the server and readying up, a room code can be specified, which can be use to coordinate readying up with other players
- Concurrent sessions - Once a randomization is generated, a random identifier is included with it which is used to spin up a new session when connecting to the server. This way, multiple concurrent rando sessions can run simultaneously on the same server

## Getting Started
1. Download the zip from the releases page on github: https://github.com/CallumMoseley/HollowKnight.RandomizerMod/releases
2. Install SeanprCore.dll and modding API if you haven't already (this can be done through the mod installer: https://www.nexusmods.com/hollowknight/mods/9)
3. Copy `MultiWorldProtocol.dll`, `RandomizerLib3.0.dll`, and `RandomizerMod3.0.dll` into `Hollow Knight/hollow_knight_Data/Managed/Mods` (this will replace the existing RandomizerMod3.0 if you have it installed
4. Download `MultiWorldServer.zip` from releases and extract it to wherever you would like to run the server from
5. Port forward 38281 to the machine running the server (look up tutorials online for your router)
6. Run `MultiWorldServer.exe`

This is all that is needed in terms of setup. To play multiworld:

1. Open Hollow Knight and start a new file
2. Enter the IP address of the server, and set Multiworld to "Yes". This will connect to the server. If the connection fails, Multiworld will stay set to "No"
3. Enter the nickname you would like to use in the "Nickname" field
4. Configure your randomizer settings however you would like
5. Enter a room code to coordinate with other players, or leave blank for the default room (easier if only one group is trying to rando at once)
6. Click "Ready" to toggle your ready status. The buttton will show how many players in the room are currently ready, and this will lock your settings in. If you would like to change settings, click again to become unready
7. Once everyone you are playing with is connected and readied up, one player should click start, and this will begin the randomizer for everyone. The player who clicks start will be marked as "Player 1", and their seed will be used for all randomization.

## Server Commands

A few useful commands are implemented on the server:
1. `ready` - Gives a list of the current rooms and how many players are ready in each
2. `list` - Lists the currently active game sessions, and the players in each
3. `give <item> <session> <playerid>` - Sends `item` to player `playerId` in session `session`. Use this if an item gets lost somehow (crash or Alt-F4)

## Gallery

![Menu MW: No](/images/menu_no.png)
![Menu MW: Yes](/images/menu_no.png)
![Send Item](/images/send.png)
![Receive Item](/images/recv.png)
![Colo Hint](/images/colo.png)

## Future Plans/Known Issues
- Item placements are currently not validated for feasibility. In theory, they should be correct as they are placed, but if there are bugs an impossible seed may be generated
- If you have an issue with Grimmchild not spawning for Grimm, then quitting out will quit without saving, potentially losing recent items. So, make sure to use Benchwarp to make Grimmchild appear before trying to fight Grimm.
- In the future, I will hopefully store all items on the server and use that to restore players when joining
