using System;

namespace PeakSentinel.Protection
{
    public readonly struct BanEntry
    {
        public BanEntry(string userId, string name, DateTime? bannedAt)
        {
            UserId = userId;
            Name = name;
            BannedAt = bannedAt;
        }

        public string UserId { get; }
        public string Name { get; }
        public DateTime? BannedAt { get; }
    }
}
