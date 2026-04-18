using MyFinBackend.Model;

namespace MyFinBackend.Dto
{
    public class ExpenseReturnDto
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public float Value { get; set; }
        public DateOnly Date { get; set; }
        public int CategoryId { get; set; }
        public int? GroupId { get; set; }
        public string? GroupName { get; set; }
    }
}
