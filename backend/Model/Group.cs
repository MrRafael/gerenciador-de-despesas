namespace MyFinBackend.Model
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string UserId { get; set; }

        public User? User { get; set; } = null!;

        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    }
}
