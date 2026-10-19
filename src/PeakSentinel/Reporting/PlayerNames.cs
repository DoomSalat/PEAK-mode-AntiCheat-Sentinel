using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Reporting
{
    public static class PlayerNames // Static: pure formatting of Photon players shared by every feature
    {
        private const string Unknown = "unknown";

        public static string Name(PhotonPlayer player)
        {
            if (player == null)
                return Unknown;

            return string.IsNullOrEmpty(player.NickName) ? $"actor {player.ActorNumber}" : player.NickName;
        }

        public static int Actor(PhotonPlayer player)
        {
            return player == null ? 0 : player.ActorNumber;
        }
    }
}