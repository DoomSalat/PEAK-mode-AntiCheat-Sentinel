using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using UnityEngine;

namespace PeakSentinel.Ui.Pages
{
    public sealed class ItemsPage : ITabPage
    {
        private const string Header = "Item spawn blocker";
        private const string BlockLabel = "Block cheat item spawns (spawners, forged pickups and inventories)";
        private const string AllowMineLabel = "Allow my own spawns (host)";
        private const string RefuseForcedLabel = "Refuse items forced into my hands (when not host)";

        private readonly FeedSection _feed;

        public ItemsPage(PageContext context, Transform page, ProtectionSettings protection)
        {
            context.Ui.Header(page, Header);
            context.Binder.Bind(page, BlockLabel, protection.BlockCheatSpawns);
            context.Binder.Bind(page, AllowMineLabel, protection.AllowMySpawns);
            context.Binder.Bind(page, RefuseForcedLabel, protection.RefuseForcedPickups);
            context.Binder.HostNote(page);
            _feed = new FeedSection(context, page, FindingCategory.Items);
        }

        public void Refresh()
        {
            _feed.Refresh();
        }
    }
}