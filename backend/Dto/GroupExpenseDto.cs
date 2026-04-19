namespace MyFinBackend.Dto
{
    public class GroupExpenseDto
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public float Value { get; set; }
        public DateOnly Date { get; set; }
        public int CategoryId { get; set; }
        public string UserId { get; set; }
        public int? GroupSplitConfigId { get; set; }
    }
}
