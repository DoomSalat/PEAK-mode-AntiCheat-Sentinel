using PeakSentinel.Network;
using PeakSentinel.Protection;
using UnityEngine;

namespace PeakSentinel.Ui.Pages
{
    public sealed class PlayersPage : ITabPage
    {
        private readonly PlayerListView _players;

        public PlayersPage(PageContext context, Transform page, BanGuard banGuard, AvatarCache avatars, MuteList mutes, LatencyMeter latency, DesyncFixer desync)
        {
            context.Binder.HostNote(page);
            _players = new PlayerListView(context.Ui, page, context.Findings, context.Filter, banGuard, avatars, mutes, latency, desync, context.Fix);
        }

        public void Refresh()
        {
            _players.Refresh();
        }
    }
}