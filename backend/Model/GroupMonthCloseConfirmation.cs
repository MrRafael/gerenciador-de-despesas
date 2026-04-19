namespace MyFinBackend.Model
{
    public class GroupMonthCloseConfirmation
    {
        public int GroupMonthCloseId { get; set; }
        public string UserId { get; set; } = null!;
        public DateTime ConfirmedAt { get; set; }

        public GroupMonthClose? GroupMonthClose { get; set; }
        public User? User { get; set; }
    }
}
