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
        private const string FlightLabel = "Flying: staying in the air without gravity";
        private const string HeuristicWarning = "(heuristic: may be wrong)";
        private const string HarmHint = "the attacker is logged; Block: the command does nothing on your side, others are safe only with their own Sentinel";
        private const string WorldHint = "the non-host is logged; Block: the event does not happen on your side, others need their own Sentinel";
        private const string DangerousHint = "the spawner is logged; Block: bees, tornados and Bing Bong are not created on your side";
        private const string FlightHint = "flyers are logged; Snap back: the flyer is teleported back and knocked down for everyone";
        private const string StaminaHint = "players whose stamina never goes down are logged";
        private const string EffectsHint = "protection or stamina effects with no source are logged";
        private const string CheatModsHint = "Cherry and Atlas users are logged; Kick: they are kicked from the room";
        private const string FlightWarning = "(heuristic: detection and snap back may be wrong)";
        private const string CheatModsLabel = "Cherry and Atlas cheat mod markers";
        private const string EffectsLabel = "Protection or stamina effect with no source (milk, golden Bing Bong...), cannot be blocked";
        private const string StaminaLabel = "Infinite stamina: sprinting or climbing while stamina does not go down, cannot be blocked";

        private readonly FeedSection _feed;

        public CheatsPage(PageContext context, Transform page, DetectionSettings detection)
        {
            context.Ui.Header(page, Header);
            SettingsBinder binder = context.Binder;
            binder.BindPair(page, HarmLabel, detection.HarmToOthers, BlockLabel, detection.BlockHarmToOthers, SecondToggleWidth);
            binder.Hint(page, 0, HarmHint, host => HintScope.You);
            binder.BindPair(page, WorldLabel, detection.HostOnlyEvents, BlockLabel, detection.BlockHostOnlyEvents, SecondToggleWidth);
            binder.Hint(page, 0, WorldHint, host => HintScope.You);
            binder.BindPair(page, DangerousLabel, detection.DangerousSpawns, BlockLabel, detection.BlockDangerousSpawns, SecondToggleWidth);
            binder.Hint(page, 0, DangerousHint, host => HintScope.You);
            binder.BindPair(page, $"{FlightLabel} {RichText.Warning(FlightWarning)}", detection.Flight, SnapBackLabel, detection.BlockFlight, SecondToggleWidth);
            binder.Hint(page, 0, FlightHint, host => host ? HintScope.Everyone : HintScope.LogOnly);
            binder.Bind(page, $"{StaminaLabel} {RichText.Warning(HeuristicWarning)}", detection.Stamina);
            binder.Hint(page, 0, StaminaHint, host => HintScope.LogOnly);
            binder.Bind(page, $"{EffectsLabel} {RichText.Warning(HeuristicWarning)}", detection.SourcelessEffects);
            binder.Hint(page, 0, EffectsHint, host => HintScope.LogOnly);
            binder.BindPair(page, CheatModsLabel, detection.CheatModMarkers, KickLabel, detection.KickCheatModUsers, SecondToggleWidth);
            binder.Hint(page, 0, CheatModsHint, host => host ? HintScope.Everyone : HintScope.LogOnly);
            _feed = new FeedSection(context, page, FindingCategory.Cheats);
        }

        public void Refresh()
        {
            _feed.Refresh();
        }
    }
}