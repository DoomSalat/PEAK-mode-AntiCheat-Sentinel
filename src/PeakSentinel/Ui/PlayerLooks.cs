using PeakSentinel.Reporting;
using UnityEngine;
using UnityEngine.UI;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Ui
{
    public sealed class PlayerLooks
    {
        private const string NoBody = "no body yet";
        private const string Dead = "dead";
        private const string KnockedOut = "knocked out";
        private const string Alive = "alive";

        private readonly AvatarCache _avatars;

        public PlayerLooks(AvatarCache avatars)
        {
            _avatars = avatars;
        }

        public int Version => _avatars.Version;

        public void ShowAvatar(Image image, PhotonPlayer player)
        {
            Sprite avatar = _avatars.Of(player);

            if (avatar == null || image.sprite == avatar)
                return;

            image.sprite = avatar;
            image.rectTransform.localScale = new Vector3(1f, -1f, 1f);
        }

        public Color ColorOf(PlayerStanding standing)
        {
            switch (standing)
            {
                case PlayerStanding.Cheater:
                    return UiPalette.Cheat;

                case PlayerStanding.Suspect:
                    return UiPalette.Suspicious;

                default:
                    return UiPalette.Clean;
            }
        }

        public (string Text, Color Color) LifeOf(PhotonPlayer player)
        {
            Character character = PlayerHandler.GetPlayerCharacter(player);

            if (character == null)
                return (NoBody, UiPalette.Notice);

            if (character.data.dead)
                return (Dead, UiPalette.Cheat);

            return character.data.passedOut || character.data.fullyPassedOut ? (KnockedOut, UiPalette.Suspicious) : (Alive, UiPalette.Clean);
        }
    }
}
