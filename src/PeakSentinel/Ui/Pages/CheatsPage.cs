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
        private const string MuteLabel = "Mute them";
        private const string FloodLabel = "Network flood: too many commands or spawns per second (lag and crash cheats)";
        private const string TakeoverLabel = "Taking control of another player's character";
        private const string FloodHint = "the flooder is logged; Mute them: everything they send is ignored on your side for this session, voice included";
        private const string TakeoverHint = "the player is logged; Block: the takeover is ignored on your side";
        private const string EffectsLabel = "Protection or stamina effect with no source (milk, golden Bing Bong...), cannot be blocked";
        private const float SettingsHeight = 430f;
        private const string ForcedLabel = "Forcing another player to jump, crouch, let go, drop items, emote or change statuses";
        private const string DestroyLabel = "Destroying objects that belong to another player";
        private const string BuildLabel = "Building with someone else's item or build spam (flags...)";
        private const string NameLabel = "Name or Steam ID spoofing, renaming mid-game";
        private const string ForcedHint = "the player is logged; Block: the forced action does not happen on your side";
        private const string DestroyHint = "the player is logged; Block: the object is not destroyed on your side";
        private const string BuildHint = "the builder is logged; Block: the build does not appear on your side";
        private const string NameHint = "players with a fake name or someone else's Steam ID are logged";
        private const string StaminaLabel = "Infinite stamina: sprinting or climbing while stamina does not go down, cannot be blocked";

        private readonly FeedSection _feed;

        public CheatsPage(PageContext context, Transform page, DetectionSettings detection)
        {
            context.Ui.Header(page, Header);
            (ScrollRect scroll, RectTransform list) = context.Ui.Scroll(page, 4f);
            context.Ui.Fixed(scroll, 0f, SettingsHeight);
            context.Ui.Flexible(scroll, 1f, 0f);
            SettingsBinder binder = context.Binder;
            binder.BindPair(list, HarmLabel, detection.HarmToOthers, BlockLabel, detection.BlockHarmToOthers, SecondToggleWidth);
            binder.Hint(list, 0, HarmHint, host => HintScope.You);
            binder.BindPair(list, ForcedLabel, detection.ForcedActions, BlockLabel, detection.BlockForcedActions, SecondToggleWidth);
            binder.Hint(list, 0, ForcedHint, host => HintScope.You);
            binder.BindPair(list, WorldLabel, detection.HostOnlyEvents, BlockLabel, detection.BlockHostOnlyEvents, SecondToggleWidth);
            binder.Hint(list, 0, WorldHint, host => HintScope.You);
            binder.BindPair(list, DangerousLabel, detection.DangerousSpawns, BlockLabel, detection.BlockDangerousSpawns, SecondToggleWidth);
            binder.Hint(list, 0, DangerousHint, host => HintScope.You);
            binder.BindPair(list, DestroyLabel, detection.ObjectDestroy, BlockLabel, detection.BlockObjectDestroy, SecondToggleWidth);
            binder.Hint(list, 0, DestroyHint, host => HintScope.You);
            binder.BindPair(list, TakeoverLabel, detection.CharacterTakeover, BlockLabel, detection.BlockCharacterTakeover, SecondToggleWidth);
            binder.Hint(list, 0, TakeoverHint, host => HintScope.You);
            binder.BindPair(list, BuildLabel, detection.BuildSpam, BlockLabel, detection.BlockBuildSpam, SecondToggleWidth);
            binder.Hint(list, 0, BuildHint, host => HintScope.You);
            binder.BindPair(list, FloodLabel, detection.Flood, MuteLabel, detection.MuteFlooders, SecondToggleWidth);
            binder.Hint(list, 0, FloodHint, host => HintScope.You);
            binder.BindPair(list, $"{FlightLabel} {RichText.Warning(FlightWarning)}", detection.Flight, SnapBackLabel, detection.BlockFlight, SecondToggleWidth);
            binder.Hint(list, 0, FlightHint, host => host ? HintScope.Everyone : HintScope.LogOnly);
            binder.Bind(list, $"{StaminaLabel} {RichText.Warning(HeuristicWarning)}", detection.Stamina);
            binder.Hint(list, 0, StaminaHint, host => HintScope.LogOnly);
            binder.Bind(list, $"{EffectsLabel} {RichText.Warning(HeuristicWarning)}", detection.SourcelessEffects);
            binder.Hint(list, 0, EffectsHint, host => HintScope.LogOnly);
            binder.Bind(list, $"{NameLabel} {RichText.Warning(HeuristicWarning)}", detection.NameSpoofing);
            binder.Hint(list, 0, NameHint, host => HintScope.LogOnly);
            binder.BindPair(list, CheatModsLabel, detection.CheatModMarkers, KickLabel, detection.KickCheatModUsers, SecondToggleWidth);
            binder.Hint(list, 0, CheatModsHint, host => host ? HintScope.Everyone : HintScope.LogOnly);
            _feed = new FeedSection(context, page, FindingCategory.Cheats);
        }

        public void Refresh()
        {
            _feed.Refresh();
        }
    }
}