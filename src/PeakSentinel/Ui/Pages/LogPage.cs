using PeakSentinel.Reporting;
using TMPro;
using UnityEngine;

namespace PeakSentinel.Ui.Pages
{
    public sealed class LogPage : ITabPage
    {
        private const float TextSize = 20f;
        private const float CapacityWidth = 120f;
        private const string CapacityLabel = "Keep at most";
        private const string StoredFormat = "Stored findings: {0} / {1}";
        private const string ExportLabel = "Export";
        private const float ExportWidth = 140f;
        private const string SavedFormat = "Saved: {0}";

        private readonly PageContext _context;
        private readonly TextMeshProUGUI _stored;
        private readonly TMP_InputField _capacity;
        private readonly TextMeshProUGUI _saved;
        private readonly FeedSection _feed;

        public LogPage(PageContext context, Transform page)
        {
            _context = context;
            RectTransform row = context.Ui.Row(page, 10f);
            _stored = context.Ui.Label(row, string.Empty, TextSize, UiPalette.Text, false);
            context.Ui.Flexible(_stored, 1f, 0f);
            context.Ui.Label(row, CapacityLabel, TextSize, UiPalette.Text, false);
            _capacity = context.Ui.Input(row, string.Empty, TextSize, CapacityWidth);
            _capacity.contentType = TMP_InputField.ContentType.IntegerNumber;
            _capacity.onEndEdit.AddListener(Resize);
            context.Ui.Button(row, ExportLabel, ExportWidth, TextSize, Export);
            _saved = context.Ui.Label(page, string.Empty, TextSize, UiPalette.Clean, false);
            _saved.gameObject.SetActive(false);
            _feed = new FeedSection(context, page, null);
        }

        public void Refresh()
        {
            FindingLog findings = _context.Findings;
            _stored.text = string.Format(StoredFormat, findings.Count, findings.Capacity);

            if (!_capacity.isFocused)
                _capacity.SetTextWithoutNotify(findings.Capacity.ToString());

            _feed.Refresh();
        }

        private void Export()
        {
            _saved.text = string.Format(SavedFormat, _context.Export());
            _saved.gameObject.SetActive(true);
        }

        private void Resize(string value)
        {
            if (int.TryParse(value, out int capacity))
                _context.Findings.Resize(capacity);

            _capacity.SetTextWithoutNotify(_context.Findings.Capacity.ToString());
            _context.Refresh();
        }
    }
}