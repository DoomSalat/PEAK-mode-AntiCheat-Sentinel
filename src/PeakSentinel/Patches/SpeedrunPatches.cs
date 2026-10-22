using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace PeakSentinel.Patches
{
    [HarmonyPatch]
    public static class SpeedrunPatches // Static: Harmony patch class
    {
        private const string SpawnItemsMethod = nameof(Spawner.SpawnItems);

        private static Spawner _spawning; // Static: the spawner whose items are being rolled right now, read by the loot roll postfixes

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Spawner), SpawnItemsMethod)]
        [HarmonyPatch(typeof(BerryBush), SpawnItemsMethod)]
        public static void NoteSpawner(Spawner __instance)
        {
            _spawning = __instance;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(Spawner), SpawnItemsMethod)]
        [HarmonyPatch(typeof(BerryBush), SpawnItemsMethod)]
        public static void ForgetSpawner()
        {
            _spawning = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LootData), nameof(LootData.GetRandomItems))]
        public static void RecordLoot(SpawnPool spawnPool, bool canRepeat, List<GameObject> __result)
        {
            if (_spawning == null || SentinelHooks.Loot == null)
                return;

            if (_spawning is Luggage luggage)
                SentinelHooks.Loot.RecordChest(luggage.displayName, spawnPool, canRepeat, __result);
            else
                SentinelHooks.Loot.RecordMapItems(_spawning.name, spawnPool, canRepeat, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LootData), nameof(LootData.GetRandomItem))]
        public static void RecordMapItem(SpawnPool spawnPool, GameObject __result)
        {
            if (_spawning != null && !(_spawning is Luggage) && SentinelHooks.Loot != null)
                SentinelHooks.Loot.RecordMapItem(_spawning.name, spawnPool, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RunManager), nameof(RunManager.StartRun))]
        public static void StartLedger()
        {
            if (SentinelHooks.Loot != null)
                SentinelHooks.Loot.StartRun();
        }
    }
}
