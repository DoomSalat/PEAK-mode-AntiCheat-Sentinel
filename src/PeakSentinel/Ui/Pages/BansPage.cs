using System.Collections.Generic;
using System.Linq;
using PeakSentinel.Protection;
using PeakSentinel.Reporting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeakSentinel.Ui.Pages
{
    public sealed class BansPage : ITabPage
    {
        private const float HeaderSize = 24f;
        private const float LineSize = 19f;
        private const float UnbanWidth = 160f;
        private const string BannedHeader = "Banned players (kicked on join while you host)";
        private const string NoBans = "nobody is banned";
        private const string UnbanLabel = "Unban";
        private const float ShareWidth = 260f;
        private const string CopyLabel = "Copy list";
        private const string PasteLabel = "Add from clipboard";
        private const string CopiedFormat = "Copied {0} bans to the clipboard: send them to a friend, they press 'Add from clipboard'";
        private const string AddedFormat = "Added {0} new bans from the clipboard";

        private readonly UiFactory _ui;
        private readonly BanList _bans;
        private readonly TextMeshProUGUI _header;
        private readonly TextMeshProUGUI _shareResult;
        private readonly RectTransform _rows;
        private string _shown;

        public BansPage(UiFactory ui, Transform parent, BanList bans)
        {
            _ui = ui;
            _bans = bans;
            RectTransform top = ui.Row(parent, 10f);
            _header = ui.Label(top, string.Empty, HeaderSize, UiPalette.Text, false);
            ui.Flexible(_header, 1f, 0f);
            ui.Button(top, CopyLabel, ShareWidth, LineSize, CopyList);
            ui.Button(top, PasteLabel, ShareWidth, LineSize, AddFromClipboard);
            _shareResult = ui.Label(parent, string.Empty, LineSize, UiPalette.Clean, false);
            _shareResult.gameObject.SetActive(false);
            (ScrollRect _, RectTransform rows) = ui.Scroll(parent, 6f);
            _rows = rows;
        }

        public void Refresh()
        {
            string signature = string.Join("|", _bans.Entries.Select(entry => entry.Key));

            if (signature == _shown)
                return;

            _shown = signature;
            _header.text = $"{BannedHeader}: {_bans.Count}";

            for (int i = _rows.childCount - 1; i >= 0; i--)
                Object.Destroy(_rows.GetChild(i).gameObject);

            if (_bans.Count == 0)
                _ui.Label(_rows, NoBans, LineSize, UiPalette.Notice, false);

            foreach (KeyValuePair<string, string> entry in _bans.Entries.ToList())
                AddRow(entry.Key, entry.Value);
        }

        private void AddRow(string userId, string name)
        {
            RectTransform row = _ui.Row(_rows, 10f);
            TextMeshProUGUI label = _ui.Label(row, $"{name}   ({userId})", LineSize, UiPalette.Cheat, false);
            _ui.Flexible(label, 1f, 0f);
            _ui.Button(row, UnbanLabel, UnbanWidth, LineSize, () => Unban(userId));
        }

        private void CopyList()
        {
            GUIUtility.systemCopyBuffer = _bans.Export();
            ShowResult(string.Format(CopiedFormat, _bans.Count));
        }

        private void AddFromClipboard()
        {
            ShowResult(string.Format(AddedFormat, _bans.Import(GUIUtility.systemCopyBuffer)));
            _shown = null;
            Refresh();
        }

        private void ShowResult(string text)
        {
            _shareResult.text = text;
            _shareResult.gameObject.SetActive(true);
        }

        private void Unban(string userId)
        {
            _bans.Unban(userId);
            Refresh();
        }
    }
}
