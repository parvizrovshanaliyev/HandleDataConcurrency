namespace HandleDataConcurrencies.Models;

public class TableResponse<T>
{
    public int Count { get; set; } 
    public List<T> List { get; set; } = new List<T>();
}
