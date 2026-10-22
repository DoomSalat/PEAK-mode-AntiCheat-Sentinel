using System.Collections.Generic;

namespace PeakSentinel.Speedrun
{
    public sealed class LootDraw
    {
        public LootDraw(string source, ushort item, IReadOnlyDictionary<ushort, double> odds)
        {
            Source = source;
            Item = item;
            Odds = odds;
        }

        public string Source { get; }
        public ushort Item { get; }
        public IReadOnlyDictionary<ushort, double> Odds { get; }
        public double Chance => ChanceOf(Item);

        public double ChanceOf(ushort item)
        {
            return Odds.TryGetValue(item, out double chance) ? chance : 0d;
        }
    }
}
