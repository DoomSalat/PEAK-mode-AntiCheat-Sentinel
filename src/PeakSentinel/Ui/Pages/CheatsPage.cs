using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using UnityEngine;

namespace PeakSentinel.Ui.Pages
{
    public sealed class CheatsPage : ITabPage
    {
        private const float SecondToggleWidth = 210f;
        private const string Header = "General cheat detection";
        private const string BlockLabel = "Block";
        private const string KickLabel = "Kick (host)";
        private const string SnapBackLabel = "Snap back (host)";
        private const string HarmLabel = "Kills, knock-outs, curses, teleports and flings of other players";
        private const string WorldLabel = "Host-only events sent by a non-host (end game, revive, fog, bridges...)";
        private const string DangerousLabel = "Bee swarms, tornados and Bing Bong spawned by a non-host";
        private const string FlightLabel = "Flying: staying in the air without gravity (numbers in PeakSentinel.log)";
        private const string CheatModsLabel = "Cherry and Atlas cheat mod markers";
        private const string EffectsLabel = "Protection or infinite stamina effect with no source: milk, golden Bing Bong... (cannot be blocked)";
        private const string StaminaLabel = "Infinite stamina: sprinting or climbing while stamina does not go down (cannot be blocked)";

        private readonly FeedSection _feed;

        public CheatsPage(PageContext context, Transform page, DetectionSettings detection)
        {
            context.Ui.Header(page, Header);
            context.Binder.BindPair(page, HarmLabel, detection.HarmToOthers, BlockLabel, detection.BlockHarmToOthers, SecondToggleWidth);
            context.Binder.BindPair(page, WorldLabel, detection.HostOnlyEvents, BlockLabel, detection.BlockHostOnlyEvents, SecondToggleWidth);
            context.Binder.BindPair(page, DangerousLabel, detection.DangerousSpawns, BlockLabel, detection.BlockDangerousSpawns, SecondToggleWidth);
            context.Binder.BindPair(page, FlightLabel, detection.Flight, SnapBackLabel, detection.BlockFlight, SecondToggleWidth);
            context.Binder.Bind(page, StaminaLabel, detection.Stamina);
            context.Binder.Bind(page, EffectsLabel, detection.SourcelessEffects);
            context.Binder.BindPair(page, CheatModsLabel, detection.CheatModMarkers, KickLabel, detection.KickCheatModUsers, SecondToggleWidth);
            _feed = new FeedSection(context, page, FindingCategory.Cheats);
        }

        public void Refresh()
        {
            _feed.Refresh();
        }
    }
}