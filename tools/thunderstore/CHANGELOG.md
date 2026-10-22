# Changelog

## Next

- Compatible with True Final Ascent: its kick key, Apocalypse weathers that lift or carry players (antigrav, flood, tsunami, avalanche, storms), mini, mega and ball modes, ghost vote buffs, the energy drink combo, late join protection, wisp gifts and the rejoin landing are no longer reported as cheats. Everything else is still checked.
- The window header shows when a True Final Ascent mode (Final Ascent, Apocalypse) is on in the lobby.
- A shield icon marks blocked actions in the in-game feed and the Log tab.

- New Speedrun tab: every chest you open as the host is logged with the game's own odds, and the luck number shows how rare this run's loot is (for example "1 in 340"). Speedrun moderators caught a cheated world record by counting exactly this by hand.
- Luck for today too: all runs since midnight while the game is open, next to the luck of the current run.
- Speedrun tab shows the mushroom table of the map: which mushroom gives which effect and a short code. The same map always gives the same code, so a different code on the same seed means the game was changed.
- Game integrity check in the Speedrun tab: other mods that patch loot, map items or mushrooms, code loaded from outside the game and BepInEx, a short code of the game DLL and of the loot tables (and a warning if the loot tables change while you play). This is what a mod whitelist cannot see.
- Speedrun tab, map: scene, biomes and, with TerrainRandomiser, its version, file code and all its settings (so "Fully random" can be checked).
- Speedrun tab, map items: luck of berries and items spawned on the map, counted apart from chests.
- Speedrun checks on yourself (optional): Sentinel's flight, speed and stamina checks, and a check that a hard landing really hurts you (the game's own fall damage formula).
- Speedrun integrity also watches game speed (slow motion, fast forward, speed hack against the system clock) and movement constants (gravity, run, jump).
- Speedrun journal (optional): a file per day with every attempt, its number and run ID, chests and alerts; each line is chained to the previous one by a hash, so an edited or removed line shows.
- Speedrun overlay: the clover luck badge now also shows the Sentinel version, attempt number, run ID and the integrity state.
- Speedrun category presets: NMG, Glitched and Any%. In Glitched and Any% the flight, speed and fall damage checks on yourself are off, because flight glitches and Glider Rest are allowed there. The category is shown on the overlay, in the journal and in the report; changing it during an attempt is an alert.
- Export run report: one file with the map, integrity, alerts, Sentinel findings about you, mushrooms, luck and every roll, to attach to a run.
- Everything in the Speedrun tab is off by default: tick "Track loot luck" (and the badge box) to use it.

## 1.0.6

- Invincibility from Scout's Perseverance (the blue healing amulet) is recognised when it is used in hand or was just put down or passed on. The 1.0.2 note about this was wrong: that change had not made it into the code.
- Status lock check skips cooked food: cooking changes what a berry does (for example a cooked flame berry), so it gave false alarms.
- Stamina check starts over when a player eats something that tops up the bonus stamina bar, instead of reporting that stamina does not go down.
- Flight check no longer flags a player standing on a narrow ledge or sliding along an edge: the game's own grounded flag now counts when the ground point it reports is real terrain right next to the player.
- Log tabs can be scrolled up again: "Follow newest" jumps to new entries only when the list is already at the bottom.
- Using the Cursed Skull is no longer reported as other players killing the holder: the game makes every player in the lobby send the holder's death, so all of them were flagged and blocked.
- Cooking at a campfire: the effects of a cooked item run in every player's game, so everyone sends them to the holder at once. Death, warp to a random player and mushroom knockback that come from cooking are no longer reported as other players attacking the holder.
- When you are not the host, blocks now say "blocked in your game only": a client can cancel a command only in its own game, other players still get it.
- In the in-game feed the "blocked" part is blue like the [Sentinel] tag: it is what Sentinel did, the rest of the line is what the player did.
- Ritual dagger used on a friend gives every other player on the map infinite stamina for 10 s; this is no longer reported as infinite stamina with no source. Area items like the friendship bugle now use their real reach.

## 1.0.5

- Internal clean-up, no change in what Sentinel detects or blocks. Checks of pushes and thrown items are faster, and access to the game's internal data is kept in one place, so the mod is quicker to fix after a PEAK update.
- The mod description now says where to send logs about a false alarm or a missed spawn.

## 1.0.4

- The manager window header always shows whether the lobby is protected (does the host run Peak Sentinel) and updates when the host changes.
- Log tab: a legend with coloured dots explains what each log colour means.
- In-game feed: the [Sentinel] tag is blue, the text is red for cheats and blocks, orange for suspicions (network floods are suspicions now) and green when protection worked (lobby protected, Fix). An ignored kick is red when kicks are off in the run and orange when the game allows them.
- The mod page explains how to send logs about a false alarm or an item spawn the host could not block.
- Players tab: a blue shield marks everyone who runs Peak Sentinel too.
- When you join a lobby, Sentinel says whether the host runs it: "lobby protected" or "the host has no Peak Sentinel, the lobby is not protected".
- On a client, items spawned through the host are marked "may be not the host" in the trace and in findings: the real spawner is only visible to the host.

## 1.0.3

- No false alarms from: snowballs, frisbees, coconuts and other thrown items (even big hits), the frog tongue, the Venus fly trap, shaky rocks and icicles, pitons and ropes placed by players breaking, reviving a skeleton, mass revive in the Void.
- Flood is not counted for 20 seconds after a campfire checkpoint moves the run to the next segment.

## 1.0.2

- Cannibalism (eating a player who allowed it) is not treated as a kill.
- Pulling a player with the rescue hook is not treated as a push.
- Flood ignores the game's per-frame status and effect syncs (for example while a flame or frost berry works) and the host's own traffic.
- No false flight on rocks, logs, mushrooms and other props: the ground check now sees them.
- Status lock is reported only when a status stays frozen. Poison wearing off, a flame berry removing cold, a frost berry removing heat and food eaten under protection are fine.
- Invincibility from the blue healing item used in hand is recognised.
- The peel you get after eating is no longer taken from your hands.
- The debug trace file is off by default (turn on [Debug] TraceFile when you need it).

## 1.0.1

- Flood counts only when a player stays over the limit 3 seconds in a row; short legit game bursts are ignored.
- No false flood or spawn alarms when you join a room in progress.
- Dropped backpacks and cooked food that loses its poison are no longer flagged.
- No false flight alarm while a player is knocked down.

## 1.0.0

- Catches PEAK-MX and CubeX style attacks found in their code: lone pushes and pulses, explosions (including the rocket blast on your character), petrification, time of day, run timer, ascent, lava and run settings changes, illegal carrying, teleporting yourself without a teleport item.
- Item give cheats: items given through your own inventory are refused, the cheater gets the real inventory back, items spawned on the ground or pulled into the hand are removed for everyone. Cloning works only with the clone amulet.
- Zombies and the Scoutmaster spawned by a non-host are removed for everyone (the removal now really reaches every player).
- New Fix actions: petrification, time of day, run timer, ascent.
- Status lock: eating something poisonous without the status rising is reported.
- PEAK-MX users are recognised by their marker, like Cherry and Atlas. Kicking a player who steals the host is on by default.
- Mute: unmute catches up everything you missed (death, ghost, objects, statuses). Resync re-applies a player's latest state. Auto-mute of flooders is off by default.
- No false alarms from artifacts and game sources: campfire, stamina and healing amulets, jump amulets, energy drink, throw-teleport item, feeding another player, the scout bugle and the Scoutmaster.
- Works with the Piggyback mod: carrying an awake player next to you and its backpack swap are not flagged.
- Fixes: false flight at spawn, half-blocked explosions, the mute cross position.

## 0.2.1

- Mute, report export, ban list sharing, ping, desync fixer, more network checks.