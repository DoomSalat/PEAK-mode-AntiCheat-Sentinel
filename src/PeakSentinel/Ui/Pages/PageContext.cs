using PeakSentinel.Reporting;
using UnityEngine.Events;

namespace PeakSentinel.Ui.Pages
{
    public sealed class PageContext
    {
        public PageContext(UiFactory ui, SettingsBinder binder, FeedFilter filter, FindingLog findings, UnityAction refresh)
        {
            Ui = ui;
            Binder = binder;
            Filter = filter;
            Findings = findings;
            Refresh = refresh;
        }

        public UiFactory Ui { get; }
        public SettingsBinder Binder { get; }
        public FeedFilter Filter { get; }
        public FindingLog Findings { get; }
        public UnityAction Refresh { get; }
    }
}