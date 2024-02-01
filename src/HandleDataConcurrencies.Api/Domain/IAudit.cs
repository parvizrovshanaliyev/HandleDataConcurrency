namespace HandleDataConcurrency.Api.Domain;

public interface IAudit
{
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}