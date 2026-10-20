# Peak Sentinel

**Tired of cheaters?** This is my answer. It is not perfect and it has limits — but cheaters live by exactly the same limits. A mod can't be perfect here, the game simply isn't built that way. You will still feel a lot safer.

Peak Sentinel watches the network. Every command in PEAK carries the name of the player who sent it, and that name is stamped by the Photon server — a cheater can't fake it. So when someone kills, teleports or spawns items, Sentinel shows **who** did it.

## What it does

| | What happens | Who is protected |
|---|---|---|
| **Item spawns** | Cheat spawns, forged pickups and swapped inventories are refused, the item is never created | **everyone**, while you are the host |
| **Items forced into your hands** | The item is removed from your inventory | you |
| **Kills, knock-outs, teleports, flings** | The attacker is logged; with Block the command does nothing on your side | you (others need their own Sentinel) |
| **Revives with no source** (self-revive cheats) | The reviver is logged; with Block the revive does not happen on your side | you |
| **Host-only events** (end game, helicopter, fog, bridges...) | The sender is logged; with Block the event does not happen on your side | you |
| **Bees, tornados, Bing Bong** from a non-host | Not created on your side | you |
| **Someone takes the host** | You take it back at once, the thief can be kicked | **everyone**, while you are the host |
| **Cherry and Atlas cheat users** | Logged, and kicked if you want | **everyone**, while you are the host |
| **Flying, speed hacks** | Logged; the host can snap the flyer back to the ground | **everyone**, while you are the host (it is a guess) |
| **Infinite stamina, godmode effects without a source** | Logged | log only (it is a guess) |
| **Warps to infinity** (black-screen cheat) | Always refused | you |
| **Forcing another player** to jump, crouch, let go, drop items, emote or show fake statuses | Logged; with Block nothing happens on your side | you |
| **Destroying other players' objects**, building with their items, flag spam | Logged; with Block it does not happen on your side | you |
| **Name and Steam ID spoofing**, renaming mid-game | Logged | log only |
| **Network floods** (lag and crash cheats) | Logged, and the flooder is muted for you | you |
| **Taking over another player's character** | Logged and ignored | you |
| **Vanilla kicks** | Optional: ignore them, or stand kicked players back up | you, or everyone |
| **Kick and Ban** | Ban is permanent: a banned player is kicked from every lobby you host | **everyone**, while you are the host |

Every setting in the window tells you in plain words what happens if you turn it on and who it protects right now.

## The manager window (F6)

- **Log, Items, Cheats, Other** — findings with search, colours (orange — a guess, red — a cheat or a block) and the switches.
- **Fix** — next to findings that can be undone: revive a killed player, wake a knocked-out one, return a teleported one, relight a campfire, put the fog back.
- **Players** — every player with their Steam avatar, a status face and what they did. **Mute** (for anyone): everything that player sends is ignored on your side for the session, voice included — to you they freeze, go silent and can do nothing, even if they rejoin. In the Esc menu their avatar turns grey and crossed out. Kick and Ban for the host.
- **Bans** — your permanent ban list with Unban; copy it to the clipboard and share it with friends.
- **Export** — save the whole session report to a file.
- The window key can be changed with the button in the window header.

In the **Esc menu** a player gets an orange **?** if they behaved strangely and a pulsing red **!** if they cheated.

## The honest limits

- Each client decides for itself what to run. If a cheater kills a player who has no Sentinel, that player dies — the host can't stop a command that goes straight to the victim. **The more of your friends install Sentinel, the less a cheater can do.**
- Flying, speed and stamina checks are guesses: they can be wrong, and they say so in the log.
- A cheating host can't be caught.
- Only the game's developers could protect everyone on the server side. A mod can't change the Photon server.

## Install

Thunderstore Mod Manager or r2modman. Or by hand: put `PeakSentinel.dll` into `BepInEx/plugins/`.

Settings: `BepInEx/config/peaksentinel.cfg`. Debug log: `BepInEx/PeakSentinel.log`.

Source: [github.com/DoomSalat/PEAK-mode-AntiCheat-Sentinel](https://github.com/DoomSalat/PEAK-mode-AntiCheat-Sentinel)
