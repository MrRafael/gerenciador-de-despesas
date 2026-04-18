namespace MyFinBackend.Model
{
    public class ExpenseSplitShare
    {
        public int ExpenseId { get; set; }
        public string UserId { get; set; }
        public decimal Percentage { get; set; }

        public Expense? Expense { get; set; }
        public User? User { get; set; }
    }
}
