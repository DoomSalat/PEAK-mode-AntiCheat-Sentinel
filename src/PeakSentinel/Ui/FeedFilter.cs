namespace PeakSentinel.Ui
{
    public sealed class FeedFilter
    {
        public bool ShowNotices { get; set; } = true; // Set: toggled from any feed options row
        public bool FollowNewest { get; set; } = true; // Set: toggled from any feed options row
        public string Search { get; set; } = string.Empty; // Set: typed into any feed search field
    }
}
