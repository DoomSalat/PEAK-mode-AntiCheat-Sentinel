using PeakSentinel.Reporting;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PeakSentinel.Ui
{
    public sealed class FeedOptions
    {
        private const float TextSize = 20f;
        private const float ToggleWidth = 200f;
        private const float ClearWidth = 140f;
        private const string NoticesLabel = "Show notices";
        private const string FollowLabel = "Follow newest";
        private const string ClearLabel = "Clear";
        private const string SearchHint = "Search nick or text...";

        private readonly FeedFilter _filter;
        private readonly Toggle _notices;
        private readonly Toggle _follow;
        private readonly TMP_InputField _search;

        public FeedOptions(UiFactory ui, Transform parent, FeedFilter filter, FindingLog findings, UnityAction changed)
        {
            _filter = filter;
            RectTransform row = ui.Row(parent, 10f);
            _notices = ui.Toggle(row, NoticesLabel, TextSize, filter.ShowNotices, value => Apply(() => filter.ShowNotices = value, changed));
            _follow = ui.Toggle(row, FollowLabel, TextSize, filter.FollowNewest, value => filter.FollowNewest = value);
            ui.Fixed(_notices, ToggleWidth, 0f);
            ui.Fixed(_follow, ToggleWidth, 0f);
            _search = ui.Input(row, SearchHint, TextSize, 0f);
            _search.onValueChanged.AddListener(value => Apply(() => filter.Search = value, changed));
            ui.Button(row, ClearLabel, ClearWidth, TextSize, () => Apply(findings.Clear, changed));
        }

        public void Refresh()
        {
            _notices.SetIsOnWithoutNotify(_filter.ShowNotices);
            _follow.SetIsOnWithoutNotify(_filter.FollowNewest);

            if (!_search.isFocused && _search.text != _filter.Search)
                _search.SetTextWithoutNotify(_filter.Search);
        }

        private void Apply(UnityAction change, UnityAction changed)
        {
            change();
            changed();
        }
    }
}