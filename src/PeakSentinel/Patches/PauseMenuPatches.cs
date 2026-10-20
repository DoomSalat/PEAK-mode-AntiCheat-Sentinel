using HarmonyLib;
using PeakSentinel.Ui;
using Portningsbolaget;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Patches
{
    [HarmonyPatch]
    public static class PauseMenuPatches // Static: Harmony patch class
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerUI), nameof(PlayerUI.Initialize))]
        public static void AddAvatarBadge(PlayerUI __instance, PhotonPlayer player)
        {
            if (SentinelHooks.Findings == null || SentinelHooks.Mutes == null || player == null)
                return;

            if (!__instance.TryGetComponent(out AvatarBadge badge))
                badge = __instance.gameObject.AddComponent<AvatarBadge>();

            badge.Initialize(__instance.playerAvatar, player, SentinelHooks.Findings, SentinelHooks.Mutes);
        }
    }
}