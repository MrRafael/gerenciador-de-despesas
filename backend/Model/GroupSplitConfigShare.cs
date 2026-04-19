namespace MyFinBackend.Model
{
    public class GroupSplitConfigShare
    {
        public int GroupSplitConfigId { get; set; }
        public string UserId { get; set; } = null!;
        public decimal Percentage { get; set; }

        public GroupSplitConfig? GroupSplitConfig { get; set; }
        public User? User { get; set; }
    }
}
