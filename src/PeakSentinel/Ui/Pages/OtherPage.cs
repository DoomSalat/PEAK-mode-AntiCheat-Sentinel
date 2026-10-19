using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace PeakSentinel.Ui.Pages
{
    public sealed class OtherPage : ITabPage
    {
        private const string Header = "Other protection";
        private const string IgnoreKicksLabel = "Protect me: ignore vanilla kicks from other players (no ragdoll, no push)";
        private const string ShieldEveryoneLabel = "Protect everyone when I am host: stand up anyone kicked while kicks are off in this run";
        private const string ShieldAsClientLabel = "Protect everyone also when I am a client";
        private const string ShieldAsClientWarning = "(other anticheats may see this as suspicious)";

        private readonly FeedSection _feed;

        public OtherPage(PageContext context, Transform page, OtherSettings other)
        {
            SettingsBinder binder = context.Binder;
            context.Ui.Header(page, Header);
            binder.Bind(page, IgnoreKicksLabel, other.IgnoreKicks);
            Toggle everyone = binder.BindIndented(page, 1, ShieldEveryoneLabel, other.ShieldEveryoneFromKicks);
            binder.Depend(everyone, () => other.IgnoreKicks.Value);
            Toggle asClient = binder.BindIndented(page, 2, $"{ShieldAsClientLabel} <color=#{ColorUtility.ToHtmlStringRGB(UiPalette.Suspicious)}>{ShieldAsClientWarning}</color>", other.ShieldEveryoneFromKicksAsClient);
            binder.Depend(asClient, () => other.IgnoreKicks.Value && other.ShieldEveryoneFromKicks.Value);
            _feed = new FeedSection(context, page, FindingCategory.Other);
        }

        public void Refresh()
        {
            _feed.Refresh();
        }
    }
}