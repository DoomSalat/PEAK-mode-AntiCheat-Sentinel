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
        public static void AddStandingBadge(PlayerUI __instance, PhotonPlayer player)
        {
            if (SentinelHooks.Findings == null || player == null)
                return;

            if (!__instance.TryGetComponent(out StandingBadge badge))
                badge = __instance.gameObject.AddComponent<StandingBadge>();

            badge.Initialize(__instance.playerAvatar, player.ActorNumber, SentinelHooks.Findings);
        }
    }
}