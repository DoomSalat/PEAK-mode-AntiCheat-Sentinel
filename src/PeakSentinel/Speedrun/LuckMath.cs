using System.Collections.Generic;
using System.Linq;

namespace PeakSentinel.Speedrun
{
    public sealed class LuckMath
    {
        public LuckReport Evaluate(IReadOnlyList<LootDraw> draws)
        {
            List<ItemLuck> items = draws
                .GroupBy(draw => draw.Item)
                .Select(group => Measure(group.Key, group.Count(), draws))
                .OrderBy(item => item.Chance)
                .ToList();

            double rarest = items.Count == 0 ? 1d : items[0].Chance;
            return new LuckReport(System.Math.Min(1d, rarest * items.Count), items);
        }

        private ItemLuck Measure(ushort item, int count, IReadOnlyList<LootDraw> draws)
        {
            List<double> chances = draws.Select(draw => draw.ChanceOf(item)).ToList();
            return new ItemLuck(item, count, chances.Sum(), AtLeast(chances, count));
        }

        private double AtLeast(List<double> chances, int count)
        {
            double[] successes = new double[count + 1];
            successes[0] = 1d;

            foreach (double chance in chances)
            {
                successes[count] += successes[count - 1] * chance;

                for (int j = count - 1; j > 0; j--)
                    successes[j] = successes[j] * (1d - chance) + successes[j - 1] * chance;

                successes[0] *= 1d - chance;
            }

            return successes[count];
        }
    }

    public sealed class LuckReport
    {
        public LuckReport(double chance, IReadOnlyList<ItemLuck> items)
        {
            Chance = chance;
            Items = items;
        }

        public double Chance { get; }
        public IReadOnlyList<ItemLuck> Items { get; }
    }

    public sealed class ItemLuck
    {
        public ItemLuck(ushort item, int count, double expected, double chance)
        {
            Item = item;
            Count = count;
            Expected = expected;
            Chance = chance;
        }

        public ushort Item { get; }
        public int Count { get; }
        public double Expected { get; }
        public double Chance { get; }
    }
}
