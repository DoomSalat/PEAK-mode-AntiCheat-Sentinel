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
            PlatformUtility.PlatformFamily remote = FamilyOf(player);
            return remote == PlatformUtility.PlatformFamily.None || remote == PlatformManager.Platform.PlatformFamily;
        }

        public static PlatformUtility.PlatformFamily FamilyOf(PhotonPlayer player)
        {
            if (player == null || !(player.CustomProperties[PlatformProperty] is string family) || !Enum.TryParse(family, out PlatformUtility.PlatformFamily remote))
                return PlatformUtility.PlatformFamily.None;

            return remote;
        }

        public static string NameOf(PlatformUtility.PlatformFamily family)
        {
            switch (family)
            {
                case PlatformUtility.PlatformFamily.Windows:
                    return "PC";

                case PlatformUtility.PlatformFamily.OSX:
                    return "Mac";

                case PlatformUtility.PlatformFamily.Playstation:
                    return "PlayStation";

                case PlatformUtility.PlatformFamily.Xbox:
                    return "Xbox";

                case PlatformUtility.PlatformFamily.Switch:
                    return "Switch";

                case PlatformUtility.PlatformFamily.Android:
                    return "Android";

                case PlatformUtility.PlatformFamily.IOS:
                    return "iOS";

                default:
                    return string.Empty;
            }
        }
    }
}
