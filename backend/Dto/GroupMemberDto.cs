namespace MyFinBackend.Dto
{
    public class GroupMemberDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public string UserId { get; set; }
        public string OwnerId { get; set; }
        public string? MemberName { get; set; }
        public string? MemberEmail { get; set; }

        public string? OwnerName { get; set; }
        public string? OwnerEmail { get; set; }
    }
}
