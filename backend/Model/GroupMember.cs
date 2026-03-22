namespace MyFinBackend.Model
{
    public class GroupMember
    {
        public int GroupId { get; set; }
        public string UserId { get; set; }
        public bool IsActive { get; set; }

        public Group? Group { get; set; } = null!;
        public User? User { get; set; } = null!;
    }
}
