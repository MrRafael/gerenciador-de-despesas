namespace MyFinBackend.Dto
{
    public enum SplitDirection { Receiver, Payer }

    public class SplitMemberResultDto
    {
        public string UserId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal Balance { get; set; }
        public SplitDirection Direction { get; set; }
    }
}
