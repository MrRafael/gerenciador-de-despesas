namespace MyFinBackend.Model
{
    public class ExpenseSplitConfig
    {
        public int ExpenseId { get; set; }
        public int GroupSplitConfigId { get; set; }

        public Expense? Expense { get; set; }
        public GroupSplitConfig? GroupSplitConfig { get; set; }
    }
}
