namespace MyFinBackend.Model
{
    public class GroupMonthClose
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public DateTime? ClosedAt { get; set; }

        public Group? Group { get; set; }
        public ICollection<GroupMonthCloseConfirmation> Confirmations { get; set; } = [];
    }
}
