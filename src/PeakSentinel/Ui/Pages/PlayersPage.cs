using UnityEngine;

namespace PeakSentinel.Ui.Pages
{
    public sealed class PlayersPage : ITabPage
    {
        private readonly PlayerListView _players;

        public PlayersPage(PageContext context, Transform page, WindowServices services, AvatarCache avatars)
        {
            context.Binder.HostNote(page);
            _players = new PlayerListView(context.Ui, page, context.Filter, services, avatars, context.Fix);
        }

        public void Refresh()
        {
            _players.Refresh();
        }
    }
}