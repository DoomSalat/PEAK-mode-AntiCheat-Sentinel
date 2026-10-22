using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace PeakSentinel.Speedrun
{
    public sealed class LootLedger
    {
        private readonly ConfigEntry<bool> _track;
        private DateTime _day = DateTime.Now.Date;

        public LootLedger(ConfigEntry<bool> track)
        {
            _track = track;
        }

        public LootTally Run { get; } = new LootTally();
        // SAVE NOTE: Today lives only while the game runs; the RunJournal file keeps every chest of the day. LOAD: read today's CHEST lines on start → LootLedger.Today.
        public LootTally Today { get; } = new LootTally();
        public LootTally Map { get; } = new LootTally();
        public int Version { get; private set; }

        public void StartRun()
        {
            Run.Clear();
            Map.Clear();
            Roll();
            Today.CountRun();
            Version++;
        }

        public void ResetRun()
        {
            Run.Clear();
            Map.Clear();
            Version++;
        }

        public void ResetToday()
        {
            Today.Clear();
            Version++;
        }

        public void RecordChest(string source, SpawnPool requested, bool canRepeat, List<GameObject> rolled)
        {
            List<LootDraw> draws = Draws(source, requested, canRepeat, rolled);

            if (draws == null)
                return;

            Roll();
            Run.Add(draws);
            Today.Add(draws);
            Version++;
        }

        public void RecordMapItems(string source, SpawnPool requested, bool canRepeat, List<GameObject> rolled)
        {
            List<LootDraw> draws = Draws(source, requested, canRepeat, rolled);

            if (draws == null)
                return;

            Map.Add(draws);
            Version++;
        }

        public void RecordMapItem(string source, SpawnPool pool, GameObject rolled)
        {
            if (!_track.Value || rolled == null || !rolled.TryGetComponent(out Item item) || LootData.AllSpawnWeightData == null || !LootData.AllSpawnWeightData.TryGetValue(pool, out Dictionary<ushort, int> weights) || !weights.ContainsKey(item.itemID))
                return;

            Map.Add(new[] { new LootDraw(source, item.itemID, OddsOf(weights)) });
            Version++;
        }

        public string NameOf(ushort item)
        {
            return ItemDatabase.TryGetItem(item, out Item found) ? found.GetName() : item.ToString();
        }

        private List<LootDraw> Draws(string source, SpawnPool requested, bool canRepeat, List<GameObject> rolled)
        {
            SpawnPool pool = RunSettings.GetValue(RunSettings.SETTINGTYPE.ChaosItems) == 1 ? SpawnPool.All : requested;

            if (!_track.Value || rolled == null || rolled.Count == 0 || LootData.AllSpawnWeightData == null || !LootData.AllSpawnWeightData.TryGetValue(pool, out Dictionary<ushort, int> weights))
                return null;

            Dictionary<ushort, int> spawnable = Spawnable(weights);
            Dictionary<ushort, int> left = new Dictionary<ushort, int>(spawnable);
            List<LootDraw> draws = new List<LootDraw>();

            foreach (GameObject prefab in rolled)
            {
                if (prefab == null || !prefab.TryGetComponent(out Item item) || !left.ContainsKey(item.itemID))
                    continue;

                draws.Add(new LootDraw(source, item.itemID, OddsOf(left)));

                if (!canRepeat)
                    left.Remove(item.itemID);

                if (left.Count == 0)
                    left = new Dictionary<ushort, int>(spawnable);
            }

            return draws;
        }

        private void Roll()
        {
            if (DateTime.Now.Date == _day)
                return;

            _day = DateTime.Now.Date;
            Today.Clear();
        }

        private Dictionary<ushort, int> Spawnable(Dictionary<ushort, int> weights)
        {
            return weights.Where(entry => !ItemDatabase.TryGetItem(entry.Key, out Item item) || item.IsValidToSpawn()).ToDictionary(entry => entry.Key, entry => entry.Value);
        }

        private Dictionary<ushort, double> OddsOf(Dictionary<ushort, int> weights)
        {
            double total = weights.Values.Sum();
            return weights.ToDictionary(entry => entry.Key, entry => entry.Value / total);
        }
    }
}
