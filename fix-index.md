# PeakSentinel — Fix Index

1. **Obsolete (2026-10-20): WindowSkin removed when the window moved to Canvas + TMP.** `Reporting/WindowSkin.LoadGameFont` uses `Resources.FindObjectsOfTypeAll<Font>()` / `<TMP_FontAsset>()` (Rule 2: Find-методы). The game exposes no other reference to a legacy `Font` for IMGUI. The lookup runs on every window open until the font is found: if the game never loads it, every F6 press scans all loaded fonts and writes a log line.
2. **Fixed (2026-10-20): the kick is now retried every 5 s.** `Protection/BanGuard` kicks each banned actor once (`_kicked`). If the kick fails (event ignored, player stays), it is not retried until the host leaves the room.
