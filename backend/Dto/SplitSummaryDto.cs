namespace MyFinBackend.Dto
{
    public class SplitMemberResultDto
    {
        public string UserId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal AmountPaid { get; set; }
        public decimal AmountOwed { get; set; }
        public decimal Balance { get; set; }
        public decimal Percentage { get; set; }
        public string Direction { get; set; } = null!;
    }
}
