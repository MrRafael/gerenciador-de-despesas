using MyFinBackend.Model;

namespace MyFinBackend.Dto
{
    public class UpdateExpenseGroupDto
    {
        public int? GroupId { get; set; }
        public SplitType? SplitType { get; set; }
    }
}
