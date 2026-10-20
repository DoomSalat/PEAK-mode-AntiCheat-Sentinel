# Changelog

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
- Flood counts only when a player stays over the limit 3 seconds in a row (short legit game bursts are ignored); no false flood or spawn alarms when you join a room in progress; cooked food that loses its poison and dropped backpacks are not flagged.
- Fixes: false flight at spawn and while knocked down, half-blocked explosions, the mute cross position.

## 0.2.1

- Mute, report export, ban list sharing, ping, desync fixer, more network checks.