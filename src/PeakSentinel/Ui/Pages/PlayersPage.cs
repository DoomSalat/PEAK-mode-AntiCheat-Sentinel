using PeakSentinel.Protection;
using UnityEngine;

namespace PeakSentinel.Ui.Pages
{
    public sealed class PlayersPage : ITabPage
    {
        private readonly PlayerListView _players;

        public PlayersPage(PageContext context, Transform page, BanGuard banGuard, AvatarCache avatars)
        {
            context.Binder.HostNote(page);
            _players = new PlayerListView(context.Ui, page, context.Findings, context.Filter, banGuard, avatars);
        }

        public void Refresh()
        {
            _players.Refresh();
        }
    }
}