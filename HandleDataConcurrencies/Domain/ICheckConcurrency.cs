namespace HandleDataConcurrency.Domain.Documents;

public interface ICheckConcurrency
{
    public Guid RowVersion { get; set; }
}