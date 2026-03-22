namespace MyFinBackend.Model
{
    public class ExpenseCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string UserId { get; set; }

        public User? User { get; set; } = null!;

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
