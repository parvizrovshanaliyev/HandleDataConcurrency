namespace HandleDataConcurrency.Models;

public class PagingRequest 
{
    public PagingRequest()
    {
        Filters = new();
    }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public List<PagingRequestFilter> Filters { get; set; }
}


public sealed class PagingRequestFilter 
{
    public object Value { get; set; }
    public string FieldName { get; set; }
    public string EqualityType { get; set; }
}

public enum EqualityType
{
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    StartsWith,
    EndsWith,
    Contains,
    DoesNotContain
}
