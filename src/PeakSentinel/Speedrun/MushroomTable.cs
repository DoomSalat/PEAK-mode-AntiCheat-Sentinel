using System.Collections.Generic;
using System.Linq;
using Zorro.Core;

namespace PeakSentinel.Speedrun
{
    public sealed class MushroomTable
    {
        private const string CodeFormat = "X8";
        private const string NoItem = "no mushroom item";
        private const string NameSeparator = ", ";
        private const int StaminaPercentPerPoint = 5;
        private const uint HashSeed = 2166136261;
        private const uint HashPrime = 16777619;

        private static readonly string[] EffectNames = // Static: fixed names in the order of Action_RandomMushroomEffect effects
        {
            "infinite stamina 4 s", "climbs faster 5 s", "low gravity 15 s", "invincible 10 s", "heals hunger, injury, poison 15%",
            "spore blast (knockback)", "blind 60 s", "falls over 8 s", "spores +25%", "numb 60 s",
        };

        private readonly List<MushroomEntry> _entries = new List<MushroomEntry>();
        private MushroomManager _source;

        public IReadOnlyList<MushroomEntry> Entries => _entries;
        public string Code { get; private set; } = string.Empty;
        public int Version { get; private set; }

        public void Refresh()
        {
            MushroomManager manager = MushroomManager.instance;

            if (manager == _source)
                return;

            _source = manager;
            _entries.Clear();
            Code = string.Empty;
            Version++;

            if (manager == null || manager.mushroomEffects == null)
                return;

            Dictionary<int, string> names = NamesByType(manager.mushroomEffects.Length);

            for (int type = 0; type < manager.mushroomEffects.Length; type++)
            {
                int effect = manager.mushroomEffects[type];
                int stamina = type < manager.mushroomStamAmt.Length ? manager.mushroomStamAmt[type] * StaminaPercentPerPoint : 0;
                string effectName = effect >= 0 && effect < EffectNames.Length ? EffectNames[effect] : effect.ToString();
                _entries.Add(new MushroomEntry(names.TryGetValue(type, out string name) ? name : NoItem, effectName, Action_RandomMushroomEffect.GoodEffects.Contains(effect), stamina));
            }

            Code = Fingerprint(manager).ToString(CodeFormat);
        }

        private Dictionary<int, string> NamesByType(int types)
        {
            Dictionary<int, List<string>> names = new Dictionary<int, List<string>>();

            foreach (Item item in SingletonAsset<ItemDatabase>.Instance.itemLookup.Values)
            {
                if (item == null || !item.TryGetComponent(out Action_RandomMushroomEffect mushroom))
                    continue;

                int type = mushroom.mushroomTypeIndex % types;

                if (!names.TryGetValue(type, out List<string> list))
                {
                    list = new List<string>();
                    names[type] = list;
                }

                if (!list.Contains(item.GetName()))
                    list.Add(item.GetName());
            }

            return names.ToDictionary(entry => entry.Key, entry => string.Join(NameSeparator, entry.Value));
        }

        private uint Fingerprint(MushroomManager manager)
        {
            uint hash = HashSeed;

            foreach (int value in manager.mushroomEffects.Concat(manager.mushroomStamAmt))
                hash = (hash ^ (uint)value) * HashPrime;

            return hash;
        }
    }

    public sealed class MushroomEntry
    {
        public MushroomEntry(string mushrooms, string effect, bool good, int stamina)
        {
            Mushrooms = mushrooms;
            Effect = effect;
            Good = good;
            Stamina = stamina;
        }

        public string Mushrooms { get; }
        public string Effect { get; }
        public bool Good { get; }
        public int Stamina { get; }
    }
}
