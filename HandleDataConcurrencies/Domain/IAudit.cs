namespace HandleDataConcurrency.Domain.Documents;

public interface IAudit
{
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}