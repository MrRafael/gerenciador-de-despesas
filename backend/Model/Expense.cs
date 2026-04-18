namespace MyFinBackend.Model
{
    public class Expense
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public float Value { get; set; }
        public DateOnly Date { get; set; }
        public string UserId { get; set; }
        public int CategoryId { get; set; }
        public int? GroupId { get; set; }
        public SplitType? SplitType { get; set; }

        public User? User { get; set; }
        public ExpenseCategory? Category { get; set; } = null!;
        public Group? Group { get; set; }
    }
}
