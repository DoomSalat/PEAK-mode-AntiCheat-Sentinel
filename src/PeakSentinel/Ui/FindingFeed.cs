using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using PeakSentinel.Reporting;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeakSentinel.Ui
{
    public sealed class FindingFeed
    {
        private const int LinesPerChunk = 40;
        private const float FontSize = 19f;
        private const float FixWidth = 190f;
        private const float BottomSlack = 24f;
        private const string TimeFormat = "HH:mm:ss";
        private const string Empty = "no findings yet";
        private const string NothingFound = "nothing matches the search";
        private const string NoParseTag = "</?noparse>";
        private const string FixLabel = "Fix";
        private const string ConfirmFixLabel = "Not host! Fix?";
        private const string FixedLabel = "Fixed";
        private const string RepeatsFormat = "  ×{0} (last {1})";

        private static readonly Regex NoParse = new Regex(NoParseTag, RegexOptions.IgnoreCase | RegexOptions.Compiled); // Static: one compiled pattern for every feed

        private readonly List<(string Text, FindingLog.Finding Fixable)> _blocks = new List<(string Text, FindingLog.Finding Fixable)>();
        private readonly StringBuilder _builder = new StringBuilder();
        private readonly UiFactory _ui;
        private readonly ScrollRect _scroll;
        private readonly RectTransform _content;
        private readonly FindingCategory? _category;
        private readonly bool _searchable;
        private readonly Action<FindingLog.Finding> _fix;
        private (int Version, bool Notices, int Actor, string Search, bool Host) _shown = (-1, false, -1, null, false);
        private FindingLog.Finding _armed;

        public FindingFeed(UiFactory ui, Transform parent, FindingCategory? category, bool searchable, Action<FindingLog.Finding> fix)
        {
            _ui = ui;
            _category = category;
            _searchable = searchable;
            _fix = fix;
            (_scroll, _content) = ui.Scroll(parent, 0f);
        }

        public int Actor { get; set; } // Set: the profile page points the feed at another player

        public void Show(FindingLog log, FeedFilter filter)
        {
            string search = _searchable ? filter.Search.Trim() : string.Empty;
            (int Version, bool Notices, int Actor, string Search, bool Host) wanted = (log.Version, filter.ShowNotices, Actor, search, PhotonNetwork.IsMasterClient);

            if (wanted == _shown)
                return;

            bool follow = filter.FollowNewest && AtBottom();
            _shown = wanted;
            Rebuild(log, filter.ShowNotices, search);

            if (follow)
                _scroll.verticalNormalizedPosition = 0f;
        }

        private bool AtBottom()
        {
            float hidden = _content.rect.height - _scroll.viewport.rect.height;
            return hidden <= BottomSlack || _scroll.verticalNormalizedPosition * hidden <= BottomSlack;
        }

        private void Rebuild(FindingLog log, bool notices, string search)
        {
            _blocks.Clear();
            _builder.Clear();
            int lines = 0;

            foreach (FindingLog.Finding finding in log.Findings)
            {
                if ((Actor != 0 && finding.Actor != Actor) || (_category.HasValue && finding.Category != _category.Value) || (finding.Kind == FindingKind.Notice && !notices) || !Matches(finding, search))
                    continue;

                lines++;

                if (finding.Repair != null)
                {
                    Flush();
                    _blocks.Add((LineOf(finding), finding));
                    continue;
                }

                if (_builder.Length > 0)
                    _builder.Append('\n');

                _builder.Append(LineOf(finding));

                if (lines % LinesPerChunk == 0)
                    Flush();
            }

            Flush();

            if (lines == 0)
                _blocks.Add((RichText.Paint(search.Length > 0 ? NothingFound : Empty, UiPalette.Notice), null));

            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                GameObject old = _content.GetChild(i).gameObject;
                old.SetActive(false); // Destroy waits for the frame end; inactive rows already drop out of the layout measured below
                UnityEngine.Object.Destroy(old);
            }

            foreach ((string text, FindingLog.Finding fixable) in _blocks)
            {
                if (fixable == null)
                    _ui.Label(_content, text, FontSize, UiPalette.Text, true);
                else
                    AddFixRow(text, fixable);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        private void AddFixRow(string text, FindingLog.Finding finding)
        {
            RectTransform row = _ui.Row(_content, 10f);
            _ui.Flexible(_ui.Label(row, text, FontSize, UiPalette.Text, true), 1f, 0f);
            string label = finding.Fixed ? FixedLabel : finding == _armed ? ConfirmFixLabel : FixLabel;
            (Button button, TextMeshProUGUI _) = _ui.Button(row, label, FixWidth, FontSize, () => Press(finding));
            button.interactable = !finding.Fixed;
        }

        private void Press(FindingLog.Finding finding)
        {
            if (!PhotonNetwork.IsMasterClient && _armed != finding)
            {
                _armed = finding;
                _shown = (-1, false, -1, null, false);
                return;
            }

            _armed = null;
            _fix(finding);
        }

        private string LineOf(FindingLog.Finding finding)
        {
            string who = Actor == 0 ? $"{finding.Name}: " : string.Empty;
            string repeats = finding.Repeats > 1 ? string.Format(RepeatsFormat, finding.Repeats, finding.LastSeen.ToString(TimeFormat)) : string.Empty;
            string icon = finding.Kind == FindingKind.Blocked ? FeedIcons.Shield : string.Empty;
            return RichText.Paint($"[{finding.Time.ToString(TimeFormat)}] <noparse>{NoParse.Replace(who, string.Empty)}</noparse>{icon}<noparse>{NoParse.Replace(finding.Text, string.Empty)}</noparse>{repeats}", ColorOf(finding.Kind));
        }

        private bool Matches(FindingLog.Finding finding, string search)
        {
            return search.Length == 0 || finding.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || finding.Text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void Flush()
        {
            if (_builder.Length == 0)
                return;

            _blocks.Add((_builder.ToString(), null));
            _builder.Clear();
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

                case FindingKind.Fix:
                    return UiPalette.Clean;

                default:
                    return UiPalette.Notice;
            }
        }
    }
}