using BepInEx.Configuration;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using UnityEngine;
using UnityEngine.UI;

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

        private readonly PageContext _context;
        private readonly FeedSection _feed;

        public CheatsPage(PageContext context, Transform page, DetectionSettings detection)
        {
            _context = context;
            context.Ui.Header(page, Header);
            Row(page, detection.HarmToOthers, HarmLabel, detection.BlockHarmToOthers, BlockLabel);
            Row(page, detection.HostOnlyEvents, WorldLabel, detection.BlockHostOnlyEvents, BlockLabel);
            Row(page, detection.DangerousSpawns, DangerousLabel, detection.BlockDangerousSpawns, BlockLabel);
            Row(page, detection.Flight, FlightLabel, detection.BlockFlight, SnapBackLabel);
            context.Binder.Bind(page, StaminaLabel, detection.Stamina);
            context.Binder.Bind(page, EffectsLabel, detection.SourcelessEffects);
            Row(page, detection.CheatModMarkers, CheatModsLabel, detection.KickCheatModUsers, KickLabel);
            _feed = new FeedSection(context, page, FindingCategory.Cheats);
        }

        public void Refresh()
        {
            _feed.Refresh();
        }

        private void Row(Transform page, ConfigEntry<bool> detect, string label, ConfigEntry<bool> block, string blockLabel)
        {
            RectTransform row = _context.Ui.Row(page, 10f);
            _context.Ui.Flexible(_context.Binder.Bind(row, label, detect), 1f, 0f);
            Toggle second = _context.Binder.Bind(row, blockLabel, block);
            _context.Ui.Fixed(second, SecondToggleWidth, 0f);
            _context.Binder.Depend(second, () => detect.Value);
        }
    }
}