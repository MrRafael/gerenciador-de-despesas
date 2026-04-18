namespace MyFinBackend.Model
{
    public class ExpenseSplitConfig
    {
        public int ExpenseId { get; set; }
        public SplitType SplitType { get; set; }

        public Expense? Expense { get; set; }
    }
}
