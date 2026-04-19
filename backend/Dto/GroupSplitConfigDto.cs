using MyFinBackend.Model;

namespace MyFinBackend.Dto
{
    public class GroupSplitConfigShareDto
    {
        public string UserId { get; set; } = null!;
        public decimal Percentage { get; set; }
    }

    public class GroupSplitConfigReturnDto
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public SplitType SplitType { get; set; }
        public bool IsDefault { get; set; }
        public List<GroupSplitConfigShareDto> Shares { get; set; } = [];
    }

    public class CreateGroupSplitConfigDto
    {
        public SplitType SplitType { get; set; }
        public bool IsDefault { get; set; }
        public List<GroupSplitConfigShareDto> Shares { get; set; } = [];
    }

    public class UpdateGroupSplitConfigDto
    {
        public bool IsDefault { get; set; }
        public List<GroupSplitConfigShareDto> Shares { get; set; } = [];
    }
}
