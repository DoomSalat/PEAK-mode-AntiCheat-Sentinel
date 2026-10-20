# Changelog

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