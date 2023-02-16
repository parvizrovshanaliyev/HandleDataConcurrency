namespace HandleDataConcurrencies.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime CreateDate { get; set; }
        public byte[] RowVersion { get; set; }
        public DateTime UpdateDate { get; set; }
    }

    public enum PaymentStatus
    {
        Waiting,
        Processing,
        Completed,
        Failed
    }
}
