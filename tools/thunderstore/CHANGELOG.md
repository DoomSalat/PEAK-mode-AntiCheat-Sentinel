# Changelog

## 1.1.5

- Ending the run through the portal at the peak is no longer blocked as "forced the game to end": any player who enters it ends the game with their own command.
- Giving a balloon to a friend (feeding it to them) is no longer reported as "tied a balloon to themselves without holding one".
- No more "knocked X down" suspicions right after you join: the falls queued while you were loading play at once, so knockdowns are not checked for 15 s after your character appears.

## 1.1.4

- Roots mushrooms are no longer reported as "status lock": their effect is random, so a status may not change after eating one.
- With the Piggyback mod: Resync also fixes a stuck carry (after a quick pick up and drop you could not pick the player up again). Resync on yourself drops your carries for everyone; Resync on another player clears their stuck carry state on your side.
- New option (off by default), Other tab: nobody can pick you up onto their back while you are not knocked out. The pick-up is refused and dropped for everyone; carrying you when you are passed out still works.
- Infinite stamina from a player's stamina aura (an artifact) is no longer reported as "no source" in the last seconds after the aura ends.

## 1.1.3

- Cross-play: the Players tab shows which platform each player is on (PC, Mac, PlayStation, Xbox, Switch, Android, iOS) with an icon.
- The Bans tab shows when each player was banned. Older bans show "ban date unknown"; shared ban lists keep their dates.
- A kick while kicks are off is logged as RECOVERED (blue shield) instead of BLOCKED when Sentinel stands the victim up for everyone: the kick itself still lands. Same on the host and on a client.
- No more "everyone knocked one player down" suspicions: some vanilla mechanics (the frog tongue, the rescue hook pulling its user) make every player's game send the same fall. A fall that your own game also sent to the same player within a second is not a finding.
- Kicks are more reliable: a cheat mod user caught right after joining was not kicked (the game's kick failed because the player was not fully loaded yet). Now every kick (cheat mods, bans, host thieves, the Kick button) works for a player who just joined and is repeated every 5 s while the player is still in the room.
- A portable stove burning down is no longer reported as "tried to extinguish a campfire": its owner's game puts it out by itself. Campfires on the map are still host-only.
- RECOVERED instead of BLOCKED wherever the host undoes something that already happened: taking the host back, removing cheat-spawned items, balloons, character clones and extra shots for everyone, returning a flyer to the ground.
- When Sentinel kicks someone (cheat mod users, banned players, host thieves) the finding says "kicked:" in blue instead of "blocked:". Kicking a host thief is now logged too.
- Players tab: a player whose name keeps changing (animated nickname) or new findings no longer rebuild the list every moment, so its buttons stay usable. Names, status and findings update in place.
- Name changes mid-game are one finding per player with a counter (x N) instead of a new line for every name.
- Players tab: a player's profile has a voice volume slider (0–200%). It is the same volume as the game's slider in the Esc menu.

## 1.1.2

- More crash protection: broken run settings (also when a client asks the host to start the run), broken map item data and NaN or infinite body positions are always refused. Hiding the map's loose items is now host-only.
- Invisibility (Admin_menukz) is now really undone: the invisible player's body is moved to where they stand. In 1.1.1 it was only reported.

## 1.1.1

- A skeleton killed by a cheater is revived as a skeleton again: the host gives the skeleton back a second after the last kill (the victim's own game resets it on death; a kill spam keeps pushing it back) and logs it separately. No extra curse is added: the revive carries no revival penalty.
- Being shot from a scout cannon is no longer reported as flying or moving too fast.
- Jetpack and rocket pack flights are no longer reported as flying or moving too fast.
- Flood check: wind on the Alpine biome makes every player send a force command about 50 times a second (even with no wind); these no longer count as a flood. Flood findings now list the commands sent most.
- New check: a player who knocks someone else down with no kick and nothing thrown (cheat ragdoll, for example 20 s again and again) is blocked; the host stands the victim back up for everyone. Waterfalls that knock players down count as fair.
- Venus fly trap: the game makes every player pull the trapped one, so these pulls (and the burst right after the victim gets free) are no longer reported as pushes or as a flood. Spiders and cacti were checked: only the trapped player sends their pull, nothing to fix there.
- Stamina check: hanging still on a vine or chain, resting on a gentle wall and climbing with little stamina left all spend stamina very slowly, which was read as "stamina does not go down". Any steady drop now counts; ladders and other cheap climbing surfaces and nearly empty stamina (10 % or less) are not checked.
- Status lock check no longer looks at heat and cold (they cancel each other and fade on their own, so a flame berry can leave heat where it was) or at a player whose status bar is already full.
- Crash protection: broken thorn, status, effect and inventory data is checked before the game reads it and is always blocked (a cheater was crashing lobbies with broken thorn data). Another player sending your effects or thorns is reported like fake statuses.
- Flood check now counts status, effect and thorn updates when they are sent for someone else's character.
- Banned players are muted at once, on the host and on clients: the game's kick only asks the player to leave and a cheater can ignore it, but muting drops everything they send on your side.
- The trace of the previous game is kept as `PeakSentinel-prev.log`, so it survives a crash and a restart.
- Spawned-item attacks: after a player spawns an item out of nowhere, their items no longer excuse pushes and knock-downs for 20 s, and even short knock-downs from them are blocked (the host stands the victim back up). A cheater dropping spawned coconuts on players was knocking them down this way. Both halves of such a coconut are now removed too (one half used to pass as a piece of a broken item).
- Kick protection is on by default for new installs (IgnoreKicks and ShieldEveryoneFromKicks): kicks are ignored while the run has kicks off, and the host stands kicked players back up. Honest kicks in a run with kicks on still work.
- Players running PeakConsole (an admin console mod) are spotted on join and logged as a suspicion, never kicked: honest hosts use it too, but it can teleport and spawn items.
- "Made X get statuses" now lists healing too (negative values) and says it is a host-only command that the game ignores from other players.
- Cheat kills: the victim's own game still kills them, so blocking the kill only on your side left them a ghost that the host saw as alive, and statues could not revive them. Now the host revives the victim for everyone right away, and a client only reports the kill (with a revive button) instead of blocking it. A kill aimed at you is still blocked.
- Cheat teleports of other players: like kills, blocking only on your side did not help (the victim's own game moves them). The host now sends the victim back for everyone; a client only reports it.
- The "play dead" and backflip emotes no longer make everyone else look like they knocked the player down (the game sends that fall from every player).
- Self-zombify: a player who turns themselves into a zombie without spores (the console Zombify command or a cheat) is reported and blocked; as host the zombie is not spawned.
- Ignored kicks and blocked knock-downs on you now also stand you back up on everyone else's screen: the fall reached them too, and the game does not sync falls, so they kept seeing you lying down.
- Rescue helicopter: lighting the flare at the peak is no longer blocked. The game sends the helicopter call from every player who sees the flare holder at the peak, and Sentinel checked a point 10 m in front of the flare instead of the holder.
- A blocked cheat kill on you now also brings you back on everyone else's screen (the kill reached them too); other Sentinel users accept that revive.
- Endless uses: shots of items with limited uses are counted per item. Firing a blowgun dart or an AntiZooka antigravity shot more times than the item allows is reported and blocked (as host, the extra antigravity field is removed for everyone).
- A player who changes their name mid-game is reported again (the Steam name check hid it when the new name was the Steam name).
- No more "NoHunger with no source" for a player who just joined late at a campfire (their body is still out of sync on your side for a few seconds).
- Blowgun spam: extra darts that hit another player are now undone by the host. For 20 s after the last blocked dart, whatever the victim's statuses gained (drowsiness and the like, which eats the stamina bar) is taken back. A client only reports these darts, since the victim's own game applies them.
- New Resync button on your own row in Players: if others keep seeing you knocked down, passed out or with wrong statuses, it stands you up and wakes you up on their screens and sends your statuses, effects and petrification again.
- The log now tells what really happened to a victim. "blocked" means the attack never reached them (it was aimed at you). The new "recovered" (blue shield too) means the host undid it for everyone: revived, woken up, stood up, sent back. If you are a client and someone else is attacked in a way only the host can undo, it is reported without "blocked", because their own game still applies it.
- Fake banana slips (a player making someone else slip with no peel under them): the host now stands the victim back up for everyone, and a fake slip on you is undone on everyone else's screen too. A client only reports a slip aimed at another player, since the victim's own game makes them slip.
- Admin_menukz (cheat menu by KystoStudio): its bots carry its own mark, so a player spawning them is named as an Admin_menukz user. Its invisibility (the body position sent as 9999 m below the map) is reported and undone: the game also sends where the player stands or climbs, so Sentinel shows them there.
- Cheat bots: a player who spawns extra copies of their own character (bots that look like them) is reported and always blocked; the host removes the copies for everyone. Mushroom zombies made from a player (the CubeX "become zombie" cheat) are now host-only spawns like the Scoutmaster.
- Balloons: tying a balloon onto another player, tying balloons to yourself without holding one, or tying more than a bunch gives is reported, and the host removes that balloon for everyone. Popping someone else's balloon is reported as "popped a balloon" and blocked.

## 1.1.0

- Work in progress: True Final Ascent support and the Speedrun tab are new and still being tested. Please send logs if something looks wrong.
- Compatible with True Final Ascent: its kick key, Apocalypse weathers that lift or carry players (antigrav, flood, tsunami, avalanche, storms), mini, mega and ball modes, ghost vote buffs, the energy drink combo, late join protection, wisp gifts and the rejoin landing are no longer reported as cheats. Everything else is still checked.
- The window header shows when a True Final Ascent mode (Final Ascent, Apocalypse) is on in the lobby.
- A shield icon marks blocked actions in the in-game feed and the Log tab.
- Checked against the PEAK update of 22 October 2026 (Steam sign-in for Photon, cross-play). Name and Steam ID spoofing checks now skip players on another platform (console players carry their own platform ID, not a Steam ID).
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