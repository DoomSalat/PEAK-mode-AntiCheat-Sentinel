using HarmonyLib;
using Peak;

namespace PeakSentinel.Patches
{
    [HarmonyPatch]
    public static class EnvironmentPatches // Static: Harmony patch class
    {
        private const string AntiGravity = "anti-gravity sphere";
        private const string Wind = "wind zone";
        private const string PushZone = "push zone";

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
    }
}
