namespace MyFinBackend.Dto
{
    public class MonthCloseConfirmationDto
    {
        public string UserId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool Confirmed { get; set; }
    }

    public class MonthCloseStatusDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public bool IsClosed { get; set; }
        public List<MonthCloseConfirmationDto> Confirmations { get; set; } = [];
    }

    public class PendingMonthDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
