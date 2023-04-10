using System.ComponentModel.DataAnnotations;

namespace HandleDataConcurrencies.Models;

public class PaymentDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public bool IsProcessed { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTimeOffset CreateDate { get; set; }
        
    [Timestamp]
    public byte[] RowVersion { get; set; }
    public DateTime UpdateDate { get; set; }
}
