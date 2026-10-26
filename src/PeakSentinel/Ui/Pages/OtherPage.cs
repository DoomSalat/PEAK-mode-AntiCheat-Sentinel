using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace PeakSentinel.Ui.Pages
{
    public sealed class OtherPage : ITabPage
    {
        private const string Header = "Other protection";
        private const float SecondToggleWidth = 300f;
        private const string DesyncLabel = "Fix players whose body goes crazy only on my side (desync after joining)";
        private const string DesyncHint = "a player flying around and knocking you away is snapped back to where they really are, on your side; also the Resync button in Players (on your own row it resyncs you for everyone)";
        private const string ReclaimHostLabel = "Take the host back if another player takes it (when I am host)";
        private const string KickThiefLabel = "Kick the thief";
        private const string IgnoreKicksLabel = "Protect me: ignore vanilla kicks from other players (no ragdoll, no push)";
        private const string AllowHonestLabel = "Off when kicks are on in the run";
        private const string ShieldEveryoneLabel = "Protect everyone when I am host: stand up anyone kicked while kicks are off in this run";
        private const string ShieldInKickModeLabel = "Protect everyone even when kicks are on in this run (honest kicks too)";
        private const string ShieldAsClientLabel = "Protect everyone also when I am a client";
        private const string ShieldAsClientWarning = "(other anticheats may see this as suspicious)";
        private const string ReclaimHint = "if someone takes the host from you, you take it back at once; Kick the thief: they are kicked";
        private const string ProtectMeHint = "kicks from others do not knock or push you; with the right box honest kicks still work";
        private const string ShieldEveryoneHint = "anyone kicked while kicks are off in the run is stood back up (the push still lands)";
        private const string InKickModeHint = "also stands up players kicked honestly while kicks are on";
        private const string AsClientHint = "the protection of everyone also works while you are not the host";
        private const string RefuseCarryLabel = "Protect me: nobody can pick me up onto their back unless I am knocked out";
        private const string RefuseDaggerLabel = "Protect me: no ritual dagger can kill me";
        private const string RefuseDaggerHint = "a sacrifice with a ritual dagger is refused, you stay alive; the others still get the dagger's buff";
        private const string RefuseCarryHint = "a pick-up while you are awake is refused and dropped for everyone; carrying you when you are passed out still works";

        private readonly FeedSection _feed;

        public OtherPage(PageContext context, Transform page, OtherSettings other)
        {
            SettingsBinder binder = context.Binder;
            context.Ui.Header(page, Header);
            binder.Bind(page, DesyncLabel, other.FixDesyncedPlayers);
            binder.Hint(page, 0, DesyncHint, host => HintScope.You);
            binder.BindPair(page, ReclaimHostLabel, other.ReclaimHost, KickThiefLabel, other.KickHostThieves, SecondToggleWidth);
            binder.Hint(page, 0, ReclaimHint, host => host ? HintScope.Everyone : HintScope.NeedsHost);
            binder.BindPair(page, IgnoreKicksLabel, other.IgnoreKicks, AllowHonestLabel, other.AllowHonestKicks, SecondToggleWidth);
            binder.Hint(page, 0, ProtectMeHint, host => HintScope.You);
            Toggle everyone = binder.BindIndented(page, 1, ShieldEveryoneLabel, other.ShieldEveryoneFromKicks);
            binder.Depend(everyone, () => other.IgnoreKicks.Value);
            binder.Hint(page, 1, ShieldEveryoneHint, host => ShieldScope(host, other));
            Toggle inKickMode = binder.BindIndented(page, 2, ShieldInKickModeLabel, other.ShieldEveryoneInKickMode);
            binder.Depend(inKickMode, () => other.IgnoreKicks.Value && other.ShieldEveryoneFromKicks.Value);
            binder.Hint(page, 2, InKickModeHint, host => ShieldScope(host, other));
            Toggle asClient = binder.BindIndented(page, 2, $"{ShieldAsClientLabel} {RichText.Warning(ShieldAsClientWarning)}", other.ShieldEveryoneFromKicksAsClient);
            binder.Depend(asClient, () => other.IgnoreKicks.Value && other.ShieldEveryoneFromKicks.Value);
            binder.Hint(page, 2, AsClientHint, host => host ? HintScope.NotNeeded : HintScope.Everyone);
            binder.Bind(page, RefuseCarryLabel, other.RefuseCarryWhileAwake);
            binder.Hint(page, 0, RefuseCarryHint, host => HintScope.You);
            binder.Bind(page, RefuseDaggerLabel, other.RefuseDaggerSacrifice);
            binder.Hint(page, 0, RefuseDaggerHint, host => HintScope.You);
            _feed = new FeedSection(context, page, FindingCategory.Other);
        }

        public void Refresh()
        {
            _feed.Refresh();
        }

        private HintScope ShieldScope(bool host, OtherSettings other)
        {
            return host || other.ShieldEveryoneFromKicksAsClient.Value ? HintScope.Everyone : HintScope.NeedsHost;
        }
    }
}