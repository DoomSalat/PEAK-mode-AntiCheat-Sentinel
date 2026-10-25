using HarmonyLib;
using Peak;
using PeakSentinel.Game;
using Photon.Pun;
using UnityEngine;

namespace PeakSentinel.Patches
{
    [HarmonyPatch]
    public static class EnvironmentPatches // Static: Harmony patch class
    {
        private const string AntiGravity = "anti-gravity sphere";
        private const string Wind = "wind zone";
        private const string PushZone = "push zone";
        private const string CannonLaunch = "shot from a scout cannon";
        private const string Waterfall = "waterfall";
        private const string FlyTrap = "venus fly trap";
        private const float CannonFlightSeconds = 8f;
        private const string Jetpack = "jetpack";
        private const string RocketPack = "rocket pack";
        private const float RocketFlightSeconds = 8f;
        private const string PlayDeadEmote = "A_Scout_Emote_PlayDead";
        private const string BackFlipEmote = "A_Scout_Emote_BackFlip";
        private const string Emote = "emote (play dead or backflip)";
        private const float EmoteSeconds = 4f;
        private const float CannonReach = 6f;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PeakGatePortal), "Update")]
        public static void NotePeakPortal(PeakGatePortal __instance)
        {
            if (SentinelHooks.Portals != null)
                SentinelHooks.Portals.Note(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AntiSphere), "FixedUpdate")]
        public static void MarkAntiGravity(AntiSphere __instance)
        {
            if (SentinelHooks.Environment == null)
                return;

            foreach (Character character in __instance.ACTIVE_CHARACTERS)
                SentinelHooks.Environment.Mark(character, AntiGravity);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WindChillZone), "FixedUpdate")]
        public static void MarkWind(WindChillZone __instance)
        {
            if (SentinelHooks.Environment == null || !__instance.windActive)
                return;

            foreach (Character character in Character.AllCharacters)
            {
                if (character != null && __instance.windZoneBounds.Contains(character.Center))
                    SentinelHooks.Environment.Mark(character, Wind);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerMoveZone), "FixedUpdate")]
        public static void MarkPushZone(PlayerMoveZone __instance)
        {
            if (SentinelHooks.Environment == null)
                return;

            foreach (Character character in Character.AllCharacters)
            {
                if (character != null && __instance.zoneBounds.Contains(character.Center))
                    SentinelHooks.Environment.Mark(character, PushZone);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterMovement), nameof(CharacterMovement.JetpackRPC))]
        public static void MarkJetpack(CharacterMovement __instance, Character ___character, float fuel)
        {
            if (SentinelHooks.Environment != null && fuel > 0f && __instance.hasJetpack)
                SentinelHooks.Environment.Mark(___character, Jetpack);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterMovement), nameof(CharacterMovement.RocketRPC))]
        public static void MarkRocketPack(Character ___character)
        {
            BackpackSlot backpack = ___character.player.GetBackpackSlot();

            if (SentinelHooks.Environment != null && !backpack.IsEmpty() && backpack.backpackType == BackpackSlot.BackpackType.Rocketpack)
                SentinelHooks.Environment.Mark(___character, RocketPack, RocketFlightSeconds);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterAnimations), "RPCA_PlayRemove")]
        public static void MarkFallingEmote(Character ___character, string emoteName)
        {
            if (SentinelHooks.Environment != null && (emoteName == PlayDeadEmote || emoteName == BackFlipEmote))
                SentinelHooks.Environment.Mark(___character, Emote, EmoteSeconds);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VenusFlyTrap), "FixedUpdate")]
        public static void MarkFlyTrapVictim(VenusFlyTrap __instance)
        {
            Character victim = GameInternals.TargetOf(__instance);

            if (SentinelHooks.Traps != null && victim != null)
                SentinelHooks.Traps.Mark(victim, FlyTrap);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WaterfallTrigger), "OnTriggerStay")]
        public static void MarkWaterfall(WaterfallTrigger __instance, Collider other)
        {
            if (SentinelHooks.Environment != null && __instance.ragdoll && CharacterRagdoll.TryGetCharacterFromCollider(other, out Character character))
                SentinelHooks.Environment.Mark(character, Waterfall);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ScoutCannon), nameof(ScoutCannon.RPCA_LaunchTarget))]
        public static void MarkCannonLaunch(ScoutCannon __instance, int targetID)
        {
            PhotonView view = PhotonNetwork.GetPhotonView(targetID);

            if (SentinelHooks.Environment == null || view == null || !view.TryGetComponent(out Character character))
                return;

            if ((character.Center - __instance.transform.position).sqrMagnitude <= CannonReach * CannonReach)
                SentinelHooks.Environment.Mark(character, CannonLaunch, CannonFlightSeconds);
        }
    }
}
