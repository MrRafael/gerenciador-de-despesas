namespace MyFinBackend.Dto
{
    public class CreateExpenseDto
    {
        public string Description { get; set; }
        public float Value { get; set; }
        public DateOnly Date { get; set; }
        public string UserId { get; set; }
        public int CategoryId { get; set; }
        public int? GroupId { get; set; }
        public int? GroupSplitConfigId { get; set; }
    }
}
