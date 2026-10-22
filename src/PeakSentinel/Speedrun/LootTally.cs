using System.Collections.Generic;

namespace PeakSentinel.Speedrun
{
    public sealed class LootTally
    {
        private readonly List<LootDraw> _draws = new List<LootDraw>();

        public IReadOnlyList<LootDraw> Draws => _draws;
        public int Chests { get; private set; }
        public int Runs { get; private set; }

        public void Add(IEnumerable<LootDraw> draws)
        {
            _draws.AddRange(draws);
            Chests++;
        }

        public void CountRun()
        {
            Runs++;
        }

        public void Clear()
        {
            _draws.Clear();
            Chests = 0;
            Runs = 0;
        }
    }
}
