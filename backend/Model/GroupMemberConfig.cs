namespace MyFinBackend.Model
{
    public class GroupMemberConfig
    {
        public int GroupId { get; set; }
        public string UserId { get; set; } = null!;
        public decimal? Salary { get; set; }

        public GroupMember? GroupMember { get; set; }
    }
}
