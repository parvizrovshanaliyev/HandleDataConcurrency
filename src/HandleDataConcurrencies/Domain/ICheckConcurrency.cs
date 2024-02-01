namespace HandleDataConcurrency.Domain;

public interface ICheckConcurrency
{
    public Guid RowVersion { get; set; }
}