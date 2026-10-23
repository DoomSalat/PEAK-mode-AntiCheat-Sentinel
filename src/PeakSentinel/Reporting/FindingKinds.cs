namespace PeakSentinel.Reporting
{
    public static class FindingKinds // Static: pure classification of finding kinds shared by the log and the feed
    {
        public static bool IsProtection(FindingKind kind)
        {
            return kind == FindingKind.Blocked || kind == FindingKind.Recovered || kind == FindingKind.Kicked;
        }
    }
}
