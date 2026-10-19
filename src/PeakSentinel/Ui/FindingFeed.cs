using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using PeakSentinel.Reporting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeakSentinel.Ui
{
    public sealed class FindingFeed
    {
        private const int LinesPerChunk = 40;
        private const float FontSize = 19f;
        private const string TimeFormat = "HH:mm:ss";
        private const string Empty = "no findings yet";
        private const string NothingFound = "nothing matches the search";
        private const string NoParseTag = "</?noparse>";

        private static readonly Regex NoParse = new Regex(NoParseTag, RegexOptions.IgnoreCase | RegexOptions.Compiled); // Static: one compiled pattern for every feed

        private readonly List<TextMeshProUGUI> _chunks = new List<TextMeshProUGUI>();
        private readonly List<string> _texts = new List<string>();
        private readonly StringBuilder _builder = new StringBuilder();
        private readonly UiFactory _ui;
        private readonly ScrollRect _scroll;
        private readonly RectTransform _content;
        private readonly FindingCategory? _category;
        private readonly bool _searchable;
        private (int Version, bool Notices, int Actor, string Search) _shown = (-1, false, -1, null);

        public FindingFeed(UiFactory ui, Transform parent, FindingCategory? category, bool searchable)
        {
            _ui = ui;
            _category = category;
            _searchable = searchable;
            (_scroll, _content) = ui.Scroll(parent, 0f);
        }

        public int Actor { get; set; } // Set: the profile page points the feed at another player

        public void Show(FindingLog log, FeedFilter filter)
        {
            string search = _searchable ? filter.Search.Trim() : string.Empty;
            (int Version, bool Notices, int Actor, string Search) wanted = (log.Version, filter.ShowNotices, Actor, search);

            if (wanted != _shown)
            {
                _shown = wanted;
                Rebuild(log, filter.ShowNotices, search);
            }

            if (filter.FollowNewest)
                _scroll.verticalNormalizedPosition = 0f;
        }

        private void Rebuild(FindingLog log, bool notices, string search)
        {
            _texts.Clear();
            _builder.Clear();
            int lines = 0;

            foreach (FindingLog.Finding finding in log.Findings)
            {
                if ((Actor != 0 && finding.Actor != Actor) || (_category.HasValue && finding.Category != _category.Value) || (finding.Kind == FindingKind.Notice && !notices) || !Matches(finding, search))
                    continue;

                AppendLine(finding);

                if (++lines % LinesPerChunk == 0)
                    Flush();
            }

            Flush();

            if (lines == 0)
                _texts.Add($"<color=#{ColorUtility.ToHtmlStringRGB(UiPalette.Notice)}>{(search.Length > 0 ? NothingFound : Empty)}</color>");

            for (int i = 0; i < _texts.Count; i++)
                Chunk(i).text = _texts[i];

            for (int i = 0; i < _chunks.Count; i++)
            {
                if (i < _texts.Count)
                    _chunks[i].gameObject.SetActive(true);
                else
                    _chunks[i].gameObject.SetActive(false);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        private bool Matches(FindingLog.Finding finding, string search)
        {
            return search.Length == 0 || finding.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || finding.Text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void AppendLine(FindingLog.Finding finding)
        {
            if (_builder.Length > 0)
                _builder.Append('\n');

            string who = Actor == 0 ? $"{finding.Name}: " : string.Empty;
            _builder.Append($"<color=#{ColorUtility.ToHtmlStringRGB(ColorOf(finding.Kind))}>[{finding.Time.ToString(TimeFormat)}] <noparse>{NoParse.Replace(who + finding.Text, string.Empty)}</noparse></color>");
        }

        private void Flush()
        {
            if (_builder.Length == 0)
                return;

            _texts.Add(_builder.ToString());
            _builder.Clear();
        }

        private TextMeshProUGUI Chunk(int index)
        {
            while (_chunks.Count <= index)
                _chunks.Add(_ui.Label(_content, string.Empty, FontSize, UiPalette.Text, true));

            return _chunks[index];
        }

        private Color ColorOf(FindingKind kind)
        {
            switch (kind)
            {
                case FindingKind.Blocked:
                case FindingKind.Cheat:
                    return UiPalette.Cheat;

                case FindingKind.Suspicious:
                    return UiPalette.Suspicious;

                default:
                    return UiPalette.Notice;
            }
        }
    }
}
