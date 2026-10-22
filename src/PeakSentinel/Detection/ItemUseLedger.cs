using System;
using System.Collections.Generic;

namespace PeakSentinel.Detection
{
    public sealed class ItemUseLedger
    {
        private const int Slack = 1;

        private readonly Dictionary<Guid, int> _uses = new Dictionary<Guid, int>();

        public bool Overspent(Item item)
        {
            if (item == null || item.totalUses <= 0 || item.data == null)
                return false;

            Guid id = item.data.guid;
            int uses = (_uses.TryGetValue(id, out int spent) ? spent : 0) + 1;
            _uses[id] = uses;
            return uses > item.totalUses + Slack;
        }
    }
}
