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
        private const string BlockHint = "cheat item spawns and forged pickups are refused, the item is never created";
        private const string AllowMineHint = "your own spawner keeps working";
        private const string RefuseForcedHint = "an item forced into your hands is removed from your inventory";

        private readonly FeedSection _feed;

        public ItemsPage(PageContext context, Transform page, ProtectionSettings protection)
        {
            context.Ui.Header(page, Header);
            SettingsBinder binder = context.Binder;
            binder.Bind(page, BlockLabel, protection.BlockCheatSpawns);
            binder.Hint(page, 0, BlockHint, host => host ? HintScope.Everyone : HintScope.NeedsHost);
            binder.Bind(page, AllowMineLabel, protection.AllowMySpawns);
            binder.Hint(page, 0, AllowMineHint, host => host ? HintScope.You : HintScope.NeedsHost);
            binder.Bind(page, RefuseForcedLabel, protection.RefuseForcedPickups);
            binder.Hint(page, 0, RefuseForcedHint, host => host ? HintScope.NotNeeded : HintScope.You);
            _feed = new FeedSection(context, page, FindingCategory.Items);
        }

        public void Refresh()
        {
            _feed.Refresh();
        }
    }
}