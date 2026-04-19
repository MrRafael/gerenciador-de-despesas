namespace MyFinBackend.Model
{
    public class GroupSplitConfig
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public SplitType SplitType { get; set; }
        public bool IsDefault { get; set; }
        public bool IsDeleted { get; set; }

        public Group? Group { get; set; }
        public ICollection<GroupSplitConfigShare> Shares { get; set; } = [];
    }
}
