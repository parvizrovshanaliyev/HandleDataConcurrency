using System.ComponentModel.DataAnnotations;
using HandleDataConcurrency.Models;

namespace HandleDataConcurrencies.Models
{
    
    public interface ICheckConcurrencyEntity
    {
        public Guid Version { get; set; }

    }
    public class Payment : ICheckConcurrencyEntity
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public bool IsProcessed { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTimeOffset CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        
        [ConcurrencyCheck]
        public Guid Version { get; set; }
    }

    public enum PaymentStatus
    {
        Waiting,
        Processing,
        Completed,
        Failed
    }
}
