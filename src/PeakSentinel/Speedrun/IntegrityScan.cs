using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace PeakSentinel.Speedrun
{
    public sealed class IntegrityScan
    {
        private const float ScanSeconds = 5f;
        private const string CodeFormat = "X8";
        private const string PatchFormat = "{0}.{1} — {2}";
        private const uint HashSeed = 2166136261;
        private const uint HashPrime = 16777619;

        private static readonly HashSet<Type> RandomnessTypes = new HashSet<Type> // Static: fixed game types that roll loot, map items and mushroom effects
        {
            typeof(LootData), typeof(Spawner), typeof(Luggage), typeof(BerryBush), typeof(BerryVine), typeof(GroundPlaceSpawner),
            typeof(MushroomManager), typeof(Action_RandomMushroomEffect), typeof(HelperFunctions), typeof(UnityEngine.Random),
        };

        private readonly string _ownId;
        private readonly FileHash _hash = new FileHash();
        private readonly List<string> _patches = new List<string>();
        private readonly List<string> _assemblies = new List<string>();
        private string _firstTables;
        private float _nextScan;

        public IntegrityScan(string ownId)
        {
            _ownId = ownId;
        }

        public string GameVersion => Application.version;
        public string GameHash { get; private set; }
        public string TablesCode { get; private set; } = string.Empty;
        public bool TablesChanged { get; private set; }
        public IReadOnlyList<string> Patches => _patches;
        public IReadOnlyList<string> Assemblies => _assemblies;
        public int Version { get; private set; }

        public void Refresh()
        {
            if (Time.unscaledTime < _nextScan)
                return;

            _nextScan = Time.unscaledTime + ScanSeconds;

            if (GameHash == null)
                GameHash = _hash.Short(typeof(Item).Assembly.Location);

            string tables = TablesFingerprint();
            _firstTables = _firstTables ?? tables;
            List<string> patches = ForeignPatches();
            List<string> assemblies = ForeignAssemblies();
            bool changed = tables != TablesCode || !patches.SequenceEqual(_patches) || !assemblies.SequenceEqual(_assemblies) || (tables != _firstTables) != TablesChanged;

            if (!changed)
                return;

            TablesCode = tables;
            TablesChanged = tables != _firstTables;
            _patches.Clear();
            _patches.AddRange(patches);
            _assemblies.Clear();
            _assemblies.AddRange(assemblies);
            Version++;
        }

        private string TablesFingerprint()
        {
            if (LootData.AllSpawnWeightData == null)
                return string.Empty;

            uint hash = HashSeed;

            foreach (KeyValuePair<SpawnPool, Dictionary<ushort, int>> pool in LootData.AllSpawnWeightData.OrderBy(entry => entry.Key))
            {
                hash = Mix(hash, (int)pool.Key);

                foreach (KeyValuePair<ushort, int> weight in pool.Value.OrderBy(entry => entry.Key))
                    hash = Mix(Mix(hash, weight.Key), weight.Value);
            }

            return hash.ToString(CodeFormat);
        }

        private List<string> ForeignPatches()
        {
            List<string> found = new List<string>();

            foreach (MethodBase method in Harmony.GetAllPatchedMethods())
            {
                if (method.DeclaringType == null || !RandomnessTypes.Contains(method.DeclaringType))
                    continue;

                IEnumerable<string> owners = Harmony.GetPatchInfo(method).Owners.Where(owner => owner != _ownId);

                foreach (string owner in owners)
                    found.Add(string.Format(PatchFormat, method.DeclaringType.Name, method.Name, owner));
            }

            found.Sort(StringComparer.Ordinal);
            return found;
        }

        private List<string> ForeignAssemblies()
        {
            string managed = Path.GetFullPath(Paths.ManagedPath);
            string bepInEx = Path.GetFullPath(Paths.BepInExRootPath);
            List<string> found = new List<string>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string location = LocationOf(assembly);

                if (location.Length == 0 || location.StartsWith(managed, StringComparison.OrdinalIgnoreCase) || location.StartsWith(bepInEx, StringComparison.OrdinalIgnoreCase))
                    continue;

                found.Add(location);
            }

            found.Sort(StringComparer.OrdinalIgnoreCase);
            return found;
        }

        private string LocationOf(Assembly assembly)
        {
            if (assembly.IsDynamic)
                return string.Empty;

            try
            {
                return string.IsNullOrEmpty(assembly.Location) ? string.Empty : Path.GetFullPath(assembly.Location);
            }
            catch (NotSupportedException)
            {
                return string.Empty;
            }
        }

        private uint Mix(uint hash, int value)
        {
            return (hash ^ (uint)value) * HashPrime;
        }
    }
}
