using System;
using Portningsbolaget.Platforms;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Reporting
{
    public static class PlayerPlatforms // Static: pure lookup of the platform a Photon player plays on, shared by every feature
    {
        private const string PlatformProperty = "PlatformFamily";

        public static bool SharesMine(PhotonPlayer player)
        {
            PlatformUtility.PlatformFamily local = PlatformManager.Platform.PlatformFamily;
            return !(player.CustomProperties[PlatformProperty] is string family) || !Enum.TryParse(family, out PlatformUtility.PlatformFamily remote) || remote == local;
        }
    }
}
