# Peak Sentinel

**Tired of cheaters?** This is my answer. It is not perfect and it has limits — but cheaters live by exactly the same limits. A mod can't be perfect here, the game simply isn't built that way. You will still feel a lot safer.

Peak Sentinel watches the network. Every command in PEAK carries the name of the player who sent it, and that name is stamped by the Photon server — a cheater can't fake it. So when someone kills, teleports or spawns items, Sentinel shows **who** did it.

## What it does

| | What happens | Who is protected |
|---|---|---|
| **Item spawns** | Cheat spawns, forged pickups and swapped inventories are refused; items spawned on the ground are removed for everyone | **everyone**, while you are the host |
| **Items a cheater gave themselves** | The host refuses the inventory, sends them back their real one and removes the item from their hands for everyone | **everyone**, while you are the host |
| **Items forced into your hands** | The item is removed from your inventory | you |
| **Kills, knock-outs, knock-downs, teleports, fake banana slips, petrification and rocket blasts** | Aimed at you: blocked. Aimed at someone else: the host undoes it for everyone right away (revives, wakes up, stands up, sends back) and the log says "recovered" | you, or **everyone** while you are the host |
| **Flings and cheat pushes** | The attacker is logged; with Block the command does nothing on your side, and a Fix button sends the victim back | you (others need their own Sentinel) |
| **Endless item uses** (blowgun and AntiZooka spam) | Extra shots are blocked; the host removes extra antigravity fields and takes back what extra darts did to a victim | you, or **everyone** while you are the host |
| **Balloons** tied onto other players or out of nowhere, popping someone else's balloons | Logged; the host removes the balloon for everyone | you, or **everyone** while you are the host |
| **Revives with no source** (self-revive cheats) | The reviver is logged; with Block the revive does not happen on your side | you |
| **Host-only events** (end game, helicopter, fog, bridges, time of day, ascent, lava, run settings...) | The sender is logged; with Block the event does not happen on your side | you |
| **Bees, tornados, zombies, Scoutmaster, Bing Bong, cheat bots** (copies of a player) from a non-host | Not created on your side; the host removes them for everyone | you, or **everyone** while you are the host |
| **Someone takes the host** | You take it back at once, the thief can be kicked | **everyone**, while you are the host |
| **Cherry, Atlas and PEAK-MX cheat users** | Logged, and kicked if you want | **everyone**, while you are the host |
| **PeakConsole users** (admin console) | Logged as a suspicion only, honest hosts use it too | you |
| **Admin_menukz users** (spotted by the mark on their bots) and **invisibility** (position sent from under the map) | Logged; an invisible player is shown where they really stand | you |
| **Flying, speed hacks** | Logged; the host can snap the flyer back to the ground | **everyone**, while you are the host (it is a guess) |
| **Infinite stamina, godmode effects without a source, status lock** (poison eaten but no poison) | Logged | log only (it is a guess) |
| **Warps to infinity** (black-screen cheat) | Always refused | you |
| **Teleporting themselves** (to you or anywhere) | Refused on your side; the host sends them back for everyone | you, or **everyone** while you are the host |
| **Forcing another player** to jump, crouch, let go, drop items, emote or show fake statuses; **carrying** a far-away player (Piggyback mod carries are fine) | Logged; with Block nothing happens on your side | you |
| **Destroying other players' objects**, building with their items, flag spam | Logged; with Block it does not happen on your side | you |
| **Name and Steam ID spoofing**, renaming mid-game | Logged | log only |
| **Network floods** (lag and crash cheats) | Logged; optionally the flooder is muted for you | you |
| **Broken status, effect, thorn and inventory data** (lobby crash cheat) | Always refused before the game reads it | you |
| **Taking over another player's character** | Logged and ignored | you |
| **A player flying around the map and knocking you away** (PEAK desync bug after joining, only on your screen) | Their body is snapped back to where they really are, automatically or with the Resync button; Resync also re-applies their latest state (statuses, effects, item in hand) | you |
| **Others keep seeing you knocked down or passed out** | The Resync button on your own row stands you up and wakes you up on their screens and sends your statuses again | **everyone** |
| **Vanilla kicks** | On by default: kicks while the run has kicks off are ignored, and as host you stand kicked players back up | you, and everyone while you are the host |
| **Kick and Ban** | Ban is permanent: a banned player is kicked from every lobby you host | **everyone**, while you are the host |

Every setting in the window tells you in plain words what happens if you turn it on and who it protects right now.

## The manager window (F6)

- **Log, Items, Cheats, Other** — findings with search, colours (red — a cheat or a block, orange — a guess, green — a fix you applied, brown — info) explained by a legend at the top of the Log tab, and the switches.
- **Fix** — next to findings that can be undone: revive a killed player, wake a knocked-out one, return a teleported one, relight a campfire, put the fog back, put the time of day and the run timer back.
- **Players** — every player with their Steam avatar, a status face, a blue shield if they run Peak Sentinel too, what they did and their ping (lag or cheats?). **Mute** (for anyone): everything that player sends is ignored on your side for the session, voice included — to you they freeze, go silent and can do nothing, even if they rejoin. On Unmute everything they did meanwhile is caught up: statuses, effects, death or revive, their ghost, objects they made or removed. In the Esc menu their avatar turns grey and crossed out. Kick and Ban for the host.
- **Bans** — your permanent ban list with Unban; copy it to the clipboard and share it with friends.
- **Export** — save the whole session report to a file.
- The window header always shows whether your lobby is protected: green when the host runs Peak Sentinel (or you are the host), orange when the host has none. It updates when the host changes.
- The window key can be changed with the button in the window header.

When you join a lobby, Sentinel tells you whether the host runs it too: **lobby protected**, or **the host has no Peak Sentinel, the lobby is not protected**.

In the **Esc menu** a player gets an orange **?** if they behaved strangely and a pulsing red **!** if they cheated.

## The honest limits

- Each client decides for itself what to run. If a cheater kills a player who has no Sentinel, that player dies — the host can't stop a command that goes straight to the victim, but a host with Sentinel revives them right after. **The more of your friends install Sentinel, the less a cheater can do.**
- Flying, speed and stamina checks are guesses: they can be wrong, and they say so in the log.
- A cheating host can't be caught.
- On a client, items spawned through the host look as if the host made them (marked "may be not the host"): only the host sees who really asked.
- Only the game's developers could protect everyone on the server side. A mod can't change the Photon server.

## Speedrun tab (work in progress)

Off by default. For runners and moderators: chest luck counted with the game's own odds, the map's mushroom table, a game integrity check, optional checks on yourself and a run report to attach to a run. Still being tested.

## Works with other mods

- **True Final Ascent** (work in progress): its kick key, Apocalypse weathers, mini, mega and ball modes, ghost votes, energy drink combo, wisp gifts and the rejoin landing count as fair play. In ranked daily runs a TFA host may kick players whose mods are not on its list.
- **Piggyback**: carrying a nearby awake player and its backpack swap are fine.

## Install

Thunderstore Mod Manager or r2modman. Or by hand: put `PeakSentinel.dll` into `BepInEx/plugins/`.

Settings: `BepInEx/config/peaksentinel.cfg`. Debug log (off by default, turn on `[Debug] TraceFile`): `BepInEx/PeakSentinel.log`.

Source: [github.com/DoomSalat/PEAK-mode-AntiCheat-Sentinel](https://github.com/DoomSalat/PEAK-mode-AntiCheat-Sentinel)

## Something went wrong? Send the logs

Your logs help a lot, especially in these two cases:

- **An honest player was flagged** — Sentinel reported or blocked something that was normal play.
- **The host runs Sentinel, but an item spawn was not blocked.**

What to attach:

1. **The report**: in the F6 window press **Log → Export**. The file appears in `BepInEx/` as `PeakSentinel-report-<date>.txt`.
2. **The trace**, if you can: turn on `[Debug] TraceFile` in `BepInEx/config/peaksentinel.cfg` (and `TraceAllRpcs` for the most detail), play until it happens again, then attach `BepInEx/PeakSentinel.log`. If the game crashed or you already restarted it, the trace of the previous game is in `BepInEx/PeakSentinel-prev.log`. Zip it if it is bigger than 25 MB.
3. A few words: what happened, who did it and roughly when.

The files contain the nicknames of the players in your lobby.

Where to send them:

- **GitHub**: [open an issue](https://github.com/DoomSalat/PEAK-mode-AntiCheat-Sentinel/issues) and drag the files into it.
- **PEAK Modding Community** on Discord: [discord.com/servers/peak-modding-community](https://discord.com/servers/peak-modding-community-1363179626435707082).
