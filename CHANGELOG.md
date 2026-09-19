# Changelog

Notable changes to ValheimPlus, newest first. Each entry mirrors the patch notes
from its [GitHub release](https://github.com/Grantapher/ValheimPlus/releases).

Releases before `0.10.0.0` targeted Valheim versions that are no longer
compatible; see the [release archive](https://github.com/Grantapher/ValheimPlus/releases)
for those notes.


## 0.10.1.2 - Bug Fixes

_Released 2026-09-14_

### Patch Notes

* Fixes auto-deposit dropping outputs on the ground right after you log in or arrive back to a base.
* Fixes the AutoStack message showing the wrong number of items moved (or none at all) in some mod conflicts.
* The Auto Stack message now also says how many chests couldn't be used because another player has them open.
* `Inventory.playerInventoryRows` is now reset to `4` when "Equipment and Quick Slots" or "Extra Slots" is installed, since those mods add inventory rows themselves and clash with V+'s implementation.
* V+ no longer applies patches when `Inventory.playerInventoryRows` is left at `4` and `Inventory` is enabled. This is to help mod compatibility with other mods that affect inventory size.
* Fixes `GridAlignment` changing direction after each crop you plant with the Cultivator.
* Fixes `Procreation.pregnancyChanceMultiplier` working backwards.
* Fixes `Procreation.ignoreAlerted` not working.
* Your own config values are now saved back to your config file when you leave a server.

### Compatibility

* ✅ - `Valheim 1.0.12 (n-40)` + `BepInExPack_Valheim 5.4.2350` + `ValheimPlus 0.10.1.2`
* ⛔️ - Anything else

### Detailed Patch Notes

* auto-deposit chest selection
    * In multiplayer, only one player's game runs a machine at a time. Auto deposit and auto fuel used to pick chests as if that player were opening them. So which chests counted depended on who was running the machine, and while your game was still loading in with no character yet, no chests counted at all.
    * Machines now pick chests the same way no matter whose game runs them. A chest counts if it isn't a personal chest and, when wards are checked, it isn't under a ward the machine is outside of.
    * This covers auto deposit for kilns, smelters, furnaces, windmills, spinning wheels, eitr refineries, beehives, fermenters, and sap collectors. It also covers auto fuel for those machines, plus fires, cooking stations, and shield generators.
* Waiting for chests after loading
    * When you log in or come back to a base, machines finish the work they did while you were away almost immediately, often before the chests around them have finished loading. That output could be deleted.
        * This was avoided more or less by waiting for the player character to load (i.e. auto-deposit chest selection using your player's character), which likely gave the chests time to load as well.
    * Machines with auto deposit turned on now wait until the chests they could deposit into within range have loaded, then start as normal. This is checked once a second, and usually completes after the first second.
    * A machine gives up and starts anyway after 10 seconds, for example if a chest never finishes loading.
    * Chests a machine can't use, like personal chests and the treasure chests found around the world, aren't waited for. A machine with no usable chests in range starts right away.
    * This applies to kilns, smelters, furnaces, windmills, spinning wheels, eitr refineries, beehives, fermenters and sap collectors, and only when their `autoDeposit` setting is on. Nothing changes for machines with auto deposit off.
* Auto Stack
    * Stack All into the chest you have open used to count the items moved by how much your inventory shrank. Some mods take a few of your items out of your inventory while Stack All runs and put them back right after. That threw the count off and could make it negative. The count now comes from how much the chest grew instead.
    * Only the player in charge of a chest knows for sure whether someone has it open. Everyone else sees a shared flag, and the game doesn't always clear it, for example when the player using the chest disconnects. Auto Stack used to trust that flag and skip the chest, even when nobody was using it anymore. It now asks the chest anyway, and the player in charge refuses only if the chest really is open.
    * When a chest refuses, the game doesn't say why. If the chest still shows as open at that moment, it's counted as in use. Otherwise it's counted as unavailable.
    * The message following an auto-stack shows how many items were moved and how many chests they went into, followed by how many chests were in use and how many were unavailable when either is above zero. Unavailable covers chests that refused for another reason, didn't answer within `AutoStack.replyTimeout`, changed hands before stacking, or couldn't be read.
* Inventory row conflicts
    * Equipment and Quick Slots and Extra Slots both add their own rows to your inventory, and `Inventory.playerInventoryRows` conflicts with them. When either mod is installed, the `Inventory` section is enabled and `playerInventoryRows` is above `4`, V+ resets it to `4` and logs a warning naming the mod.
    * This check runs once every mod has finished loading, so it doesn't depend on the order mods load in. It runs again whenever V+ re-applies its own patches.
* `Inventory.playerInventoryRows` at `4`
    * `4` is the game's normal row count. At that value V+ now leaves inventory size completely alone, even with the `Inventory` section enabled, so other inventory mods and the game's own row changes work as if V+ weren't touching it.
    * Previously, V+ still resized your inventory whenever the `Inventory` section was enabled, even when it asked for nothing beyond the default.
    * The setting's description was reworded to make clear it's a minimum. Your inventory uses this value or the rows the game gives you, whichever is larger, and lowering it never takes away rows the game has added.
* Grid alignment when planting
    * Some pieces, like crops, get a random rotation each time you place one, so fields don't look uniform. The grid direction follows the placement rotation, so after every crop planted with grid alignment active, the grid turned to a new random direction.
    * While grid alignment is active, the rotation you picked is now kept after placing a piece and when the placement preview is rebuilt, for example after you run out of a seed. Rotating it yourself still works as normal.
    * With grid alignment off, crops still get their random rotation.
* `Procreation.pregnancyChanceMultiplier`
    * The game's "pregnancy chance" is actually the chance a creature *doesn't* gain a love point. V+ was multiplying that directly, so the setting did the opposite of what it says. `100` made love points rarer (usually never gained at all), and `-100` made them guaranteed.
    * The multiplier now applies to the chance of gaining a love point, as described. `100` doubles it (capped at always), and `-100` stops creatures from gaining love points.
    * If you had set this to a negative value to work around the bug, flip it back.
* `Procreation.ignoreAlerted`
    * The patch that lets tamed creatures breed while alerted read the wrong data from the creature, so the setting didn't work properly. It now checks the creature correctly, and creatures of the types in `Procreation.animalTypes` can breed while alerted.
* Config file after leaving a server
    * When you join a server that syncs its config, its values replace yours while connected, and your own come back when you leave. Your config file is now saved again at that point, so it holds your own values rather than whatever was last written while connected.


## 0.10.1.1 - Bug Fixes

_Released 2026-09-12_

### Patch Notes

* Fixes Auto Stack deleting items and eventually stopping altogether when more than one player stacks into the same chests.
* Fixes characters last saved before Valheim 1.0 losing the contents of the inventory rows added by `Inventory.playerInventoryRows`.
* Replaces `Player.baseUnarmedDamage`, which could not be set to leave the game unchanged, with `Player.unarmedDamageScale`.

### Behavior Changes

* `Player.baseUnarmedDamage` is replaced by `Player.unarmedDamageScale`. The old setting replaced unarmed damage outright and reapplied the skill scaling the game already does, so no value of it left the game's own behavior intact. The new setting is a +/- percent modifier that defaults to `0`, which changes nothing. `50` gives 50% more unarmed damage, `-50` gives half. If you had `baseUnarmedDamage` set, it no longer does anything and the key can be removed.
* Auto Stack no longer fills carts and ships. Craft From Chest still uses them as configured by `CraftFromChest.allowCraftingFromCarts` and `allowCraftingFromShips`.
* Auto Stack skips chests another player currently has open, instead of stacking into them. The game does not support two players changing a chest at once.

### Compatibility

* ✅ - `Valheim 1.0.12 (n-40)` + `BepInExPack_Valheim 5.4.2350` + `ValheimPlus 0.10.1.1`
* ⛔️ - Anything else

### Detailed Patch Notes

* Auto Stack
  * In multiplayer, only one player's game is in charge of a chest at a time, and a chest only keeps what you put in it if you are the one in charge. Auto Stack used to start filling a chest before it was actually in charge of it, so the items left your inventory and the chest threw them away. It now waits its turn first, and leaves a chest alone if its turn never comes.
  * Auto Stack used to ask for one chest's turn at a time, waiting for that chest to answer before asking the next one. If a chest never answered, it waited forever, and Auto Stack quietly stopped working until you restarted the game. It now asks every chest for a turn at once and gives up on the ones that stay quiet for too long.
  * New `AutoStack.replyTimeout` setting, default `1` second. This is how long Auto Stack waits for a chest to answer, and then how long it waits for its turn with that chest. Nothing is stacked until the waiting is over, and both waits can happen on one press, so a chest that never answers leaves you standing there for up to twice this long before anything moves. Raise it if you play on a laggy server and Auto Stack keeps skipping chests, and lower it if it consistently completes quickly but sometimes has a hiccup.
  * The Stack All message now tells you how many items were moved, how many chests they went into, and how many chests could not be used.


## 0.10.1.0 - Valheim 1.0.12 Compatibility

_Released 2026-09-11_

### Patch Notes

* Compatibility with Valheim game version `1.0.12`

### Compatibility

* ✅ - `Valheim 1.0.12 (n-40)` + `BepInExPack_Valheim 5.4.2350` + `ValheimPlus 0.10.1.0`
* ⛔️ - Anything else


## 0.10.0.3 - Bug fixes

_Released 2026-09-11_

### Patch Notes

* Adds support for [Shudnal's Configuration Manager](https://thunderstore.io/c/valheim/p/shudnal/ConfigurationManager/), alongside [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager).
* Settings with limits now show as sliders in Configuration Managers.
* Fixes game audio being quieter than it should be when the game's `Mute game in background` option is on.
* Useful troubleshooting info is back in the log by default, so `BepInEx/LogOutput.log` is usually all that's needed when reporting a problem.
* Probably a lot of things in [the 0.10.0.0 release](https://github.com/Grantapher/ValheimPlus/releases/tag/0.10.0.0) you haven't seen yet.

### Compatibility

* ✅ - `Valheim 1.0.7 (n-39)` + `BepInExPack_Valheim 5.4.2350` + `ValheimPlus 0.10.0.3`
* ⛔️ - Anything else

### Detailed Patch Notes

* Audio
  * With `Mute game in background` on, the master volume was applied twice when the game regained focus, making everything quieter.
* Logging
  * Game and Valheim Plus versions, config loading and migration, Configuration Manager detection, and server sync are logged at `Info` again.
  * Each time patches are re-applied, such as after closing the settings window or joining a server, the log lists which settings changed and what they changed from.


## 0.10.0.2 - Inventory fix

_Released 2026-09-10_

### Patch Notes

* Fixes issue where the result of applying `Inventory.playerInventoryRows` would be saved to the character.
* Probably a lot of things in [the 0.10.0.0 release](https://github.com/Grantapher/ValheimPlus/releases/tag/0.10.0.0) you haven't seen yet.

### Compatibility

* ✅ - `Valheim 1.0.7 (n-39)` + `BepInExPack_Valheim 5.4.2350` + `ValheimPlus 0.10.0.2`
* ⛔️ - Anything else


## 0.10.0.1 - Server config sync fix

_Released 2026-09-10_

### Patch Notes

* Fixes server config sync-ing.
* Updates the bundled BepInEx to `5.4.2350`.
* Probably a lot of things in [the 0.10.0.0 release](https://github.com/Grantapher/ValheimPlus/releases/tag/0.10.0.0) you haven't seen yet.

### Compatibility

* ✅ - `Valheim 1.0.7 (n-39)` + `BepInExPack_Valheim 5.4.2350` + `ValheimPlus 0.10.0.1`
* ⛔️ - Anything else

### Detailed Patch Notes

* Server config sync
  * A server sends its settings as you connect. The mod now reads them *after* they have arrived rather than
    *just before*, which is why most of them were being ignored. Only settings that are looked up as you play
    were working.
* Bundled BepInEx
  * `BepInExPack_Valheim` is now `5.4.2350`, which brings BepInEx `5.4.23.5`.


## 0.10.0.0 - Valheim 1.0 Compatibility

_Released 2026-09-10_

### Patch Notes

* Compatibility with Valheim game version `1.0.7`
* Fixes for everything 1.0 broke, including map sync, craft from chest, area repair, guardian powers, and chest sizes.
* Two new structural integrity settings, `ice` and `timberwood`, for the build materials 1.0 added.
* Configuration now uses BepInEx, so settings can be edited in the main menu with [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager).
  * Your existing `valheim_plus.cfg` is imported automatically the first time you run this version.
  * Your old `valheim_plus.cfg` is then kept as `valheim_plus.cfg.migrated`. It is not deleted.
  * If you need to continue using the old file for automation purposes, put it back after the move and it will still be used.
    * In this case, settings will be read-only in Configuration Manager.
* Keybinds, `Hud` options, and FOV are no longer sync-ed from the server. The local client settings are used instead.
* Fixes beehives clearing nearby chests on zone load.

### Behavior Changes

A few fixes changed behavior on purpose. Nothing here is a bug.

* `playerInventoryRows` is now a **minimum** rather than a fixed size, and anything above `9` does nothing.
  Valheim 1.0 keeps inventory rows on your character and lets you buy more from a trader, so rows you earn in
  game are kept instead of being reset to your configured number. `9` is the game's own limit.
* `noWeatherDamage` covers rain only. 1.0 added other kinds of weather damage that it does not cover. These may
  be added later, which may come with a rename of the setting.

### Contributors

* @Bellian for reporting the crafting errors and finding the smelter fueling break.
* @JF10R for PR contributing to this release.

### Compatibility

* ✅ - `Valheim 1.0.7 (n-39)` + `BepInExPack_Valheim 5.4.2333` + `ValheimPlus 0.10.0.0`
* ⛔️ - Anything else

### Detailed Patch Notes

* Compatibility fixes for 1.0.

#### Included from `alpha01`

Full notes at the [`alpha01` release](https://github.com/Grantapher/ValheimPlus/releases/tag/0.10.0.0-alpha01).

* `Configuration`
  * Settings now live in `BepInEx/config/org.bepinex.plugins.valheim_plus.cfg`.
* Configuration Manager Support
  * Press F1 at the main menu to edit any setting.
  * Settings are read-only while a world is loaded. You can only edit them in the main menu.
  * Closing the settings window applies your changes.
* Server config sync
  * A player joining a server takes its settings, and gets their own back when they leave.
  * Keybinds, `Hud`, `FirstPerson` and `Camera.cameraFOV` always stay the player's own. `Server.serverSyncHotkeys` is gone.
* `Beehive`
  * Fix beehives clearing the contents of nearby chests when a zone loads.
* Removed
  * The `ValheimPlus` and `Deconstruct` sections. `mainMenuLogo` and `Deconstruct` didn't do anything, and `disableConfigAutoUpdates` went with the auto-updater.
  * The in-house settings screen, replaced by Configuration Manager.
* Fixes
  * `StructuralIntegrity` settings no longer apply when that section is turned off but `Building` is turned on.
  * `Player.disableEightSecondTeleport` and `Server.maxPlayers` now turn off with their section instead of staying on.
  * `Map.exploreRadius` now stays within its limit everywhere, instead of only in some places.
* Defaults
  * A few settings started at a different value than the one written in the shipped `valheim_plus.cfg`. The
    file's value is the one that applies. This only affects fresh installs, your own values are imported
    unchanged.
* Logging
  * Valheim Plus is now quiet unless something is wrong. For a detailed log, see [`TROUBLESHOOTING.md`](https://github.com/Grantapher/ValheimPlus/blob/main/TROUBLESHOOTING.md#getting-a-detailed-log).
