using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine.SceneManagement;
using Zorro.Core;

namespace PeakSentinel.Speedrun
{
    public sealed class MapPassport
    {
        private const string RandomiserMarker = "TerrainRandom";
        private const string NameSeparator = ", ";
        private const string SettingFormat = "{0}.{1} = {2}";
        private const string PluginFormat = "{0} {1}";

        private readonly List<string> _randomiserSettings = new List<string>();
        private readonly FileHash _hash = new FileHash();

        public string Scene { get; private set; } = string.Empty;
        public string Biomes { get; private set; } = string.Empty;
        public string Randomiser { get; private set; }
        public string RandomiserHash { get; private set; }
        public IReadOnlyList<string> RandomiserSettings => _randomiserSettings;
        public int Version { get; private set; }

        public void Refresh()
        {
            string scene = SceneManager.GetActiveScene().name;
            string biomes = BiomeNames();

            if (scene == Scene && biomes == Biomes)
                return;

            Scene = scene;
            Biomes = biomes;
            ReadRandomiser();
            Version++;
        }

        private string BiomeNames()
        {
            MapHandler map = Singleton<MapHandler>.Instance;
            return map == null || map.biomes == null ? string.Empty : string.Join(NameSeparator, map.biomes.Select(biome => biome.ToString()));
        }

        private void ReadRandomiser()
        {
            _randomiserSettings.Clear();
            Randomiser = null;
            RandomiserHash = null;
            PluginInfo plugin = Chainloader.PluginInfos.Values.FirstOrDefault(info => info.Metadata.GUID.Contains(RandomiserMarker) || info.Metadata.Name.Contains(RandomiserMarker));

            if (plugin == null || plugin.Instance == null)
                return;

            Randomiser = string.Format(PluginFormat, plugin.Metadata.Name, plugin.Metadata.Version);
            RandomiserHash = _hash.Short(plugin.Location);

            foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> entry in plugin.Instance.Config)
                _randomiserSettings.Add(string.Format(SettingFormat, entry.Key.Section, entry.Key.Key, entry.Value.BoxedValue));
        }
    }
}
