namespace HandleDataConcurrency.Api.Domain;

public interface ICheckConcurrency
{
    public Guid RowVersion { get; set; }
}