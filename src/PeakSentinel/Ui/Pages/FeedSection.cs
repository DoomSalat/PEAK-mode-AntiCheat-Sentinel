using PeakSentinel.Reporting;
using UnityEngine;

namespace PeakSentinel.Ui.Pages
{
    public sealed class FeedSection
    {
        private readonly PageContext _context;
        private readonly FeedOptions _options;
        private readonly FindingFeed _feed;

        public FeedSection(PageContext context, Transform page, FindingCategory? category)
        {
            _context = context;
            _options = new FeedOptions(context.Ui, page, context.Filter, context.Findings, context.Refresh);
            _feed = new FindingFeed(context.Ui, page, category, true, context.Fix);
        }

        public void Refresh()
        {
            _options.Refresh();
            _feed.Show(_context.Findings, _context.Filter);
        }
    }
}