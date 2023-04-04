using System.ComponentModel.DataAnnotations;

namespace HandleDataConcurrency.Models
{
    public abstract class CheckConcurrencyEntity
    {
        [ConcurrencyCheck] 
        public byte[] Version { get; set; }

    }
    
    
    public class Product : CheckConcurrencyEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ProductPutRequest
    {
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        public byte[] Version { get; set; }
    }
}
