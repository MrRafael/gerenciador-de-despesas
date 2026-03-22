using MyFinBackend.Model;

namespace MyFinBackend.Dto
{
    public class BulkExpenseToSaveDto
    {
        public List<Expense> Expenses { get; set; }
    }
}
