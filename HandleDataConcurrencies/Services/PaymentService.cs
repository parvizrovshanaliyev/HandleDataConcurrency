using System.Text.Json;
using HandleDataConcurrencies.Data;
using HandleDataConcurrencies.Models;
using HandleDataConcurrency.Models;
using Microsoft.EntityFrameworkCore;

namespace HandleDataConcurrency.Services;

/// <summary>
/// uncompleted 
/// </summary>
public interface IPaymentService
{
    Task<TableResponse<PaymentDto>> GetAllAsync(PagingRequest request);
}
public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;

    public PaymentService(ApplicationDbContext context)
    {
        _context = context;

    }

    public async Task<TableResponse<PaymentDto>> GetAllAsync(PagingRequest request)
    {
        var query = from p in _context.Payments.AsQueryable()
            select new PaymentDto()
            {
                Id = p.Id,
                Status = p.Status,
                Amount = p.Amount,
                CreateDate = p.CreateDate,
                UpdateDate = p.UpdateDate,
                IsProcessed = p.IsProcessed
            };

        var payments = await AddFilter(query, request , out int count).Skip(0).Take(100).ToListAsync();

        return new TableResponse<PaymentDto>()
        {
            Count = count,
            List = payments
        };
    }
    

    private static string GetEquality(string equalityType)
    {
        return equalityType switch
        {
            nameof(EqualityType.Equal) => "==",
            nameof(EqualityType.NotEqual) => "!=",
            nameof(EqualityType.LessThan) => "<",
            nameof(EqualityType.LessThanOrEqual) => "<=",
            nameof(EqualityType.GreaterThan) => ">",
            nameof(EqualityType.GreaterThanOrEqual) => ">=",
            nameof(EqualityType.StartsWith) => ".StartsWith(@0)",
            nameof(EqualityType.EndsWith) => ".EndsWith(@0)",
            nameof(EqualityType.Contains) => ".Contains(@0)",
            nameof(EqualityType.DoesNotContain) => ".Contains(@0) == false",
            _ => "=="
        };
    }


    protected IQueryable<K> AddFilter<K>(IQueryable<K> query, PagingRequest pagingRequest, out int count)
    {
        try
        {
            foreach (var filter in pagingRequest.Filters!)
            {
                string filterFieldName = filter.FieldName;
                object filterValue = filter.Value;
                var equalityType= GetEquality(filter.EqualityType);

                string body = string.Empty;
                if (filter.EqualityType == nameof(EqualityType.StartsWith))
                {
                    body += $"{filterFieldName}.StartsWith(\"{filterValue}\")";
                }
                else if (filter.EqualityType == nameof(EqualityType.Contains))
                {
                    body += $"{filterFieldName}.Contains(\"{filterValue}\")";
                }
                else if (filter.EqualityType == nameof(EqualityType.Equal))
                {
                    bool isString = filterValue.ToString() is not null;
                    bool isDateProp = typeof(K).GetProperty(filterFieldName).PropertyType == typeof(DateTime);
                    if (isDateProp)
                    {
                        //DateTime.TryParse(filterValue.ToString(), out DateTime value);
                        //body += $"{filterFieldName}.Date {equalityType} {filterValue.ToString()}";
                        body = BuildDateExpression<K>(filterValue, body, filterFieldName, equalityType);
                    }
                    if (filterValue is int || ((JsonElement)filterValue).ValueKind == JsonValueKind.Number)
                        body += $"{filterFieldName}=={filterValue}";
                    // else if (((JsonElement)filterValue).ValueKind == JsonValueKind.String)
                    // {
                    //     if (filterValue.ToString().Equals("null"))
                    //         body += $"{filterFieldName} is null";
                    //     else
                    //         body += $"{filterFieldName}==\"{filterValue}\"";
                    // }
                    else if (((JsonElement)filterValue).ValueKind == JsonValueKind.True)
                        body += $"{filterFieldName}";
                    else if (((JsonElement)filterValue).ValueKind == JsonValueKind.False)
                        body += $"!{filterFieldName}";
                    else if (((JsonElement)filterValue).ValueKind == JsonValueKind.Null)
                        body += $"{filterFieldName} is null";
                }
                else if (filter.EqualityType == nameof(EqualityType.GreaterThan))
                {
                    if (filterValue is DateTime && DateTime.TryParse(filterValue.ToString(), out DateTime value))
                        body += $"{filterFieldName}.Date > {value.Date}";
                }
                else if (filter.EqualityType == nameof(EqualityType.GreaterThanOrEqual))
                {
                    body = BuildDateExpression<K>(filterValue, body, filterFieldName, equalityType);
                }
                else if (filter.EqualityType == nameof(EqualityType.LessThan))
                {
                    if (filterValue is DateTime && DateTime.TryParse(filterValue.ToString(), out DateTime value))
                        body += $"{filterFieldName}.Date < {value.Date}";
                }
                else if (filter.EqualityType == nameof(EqualityType.LessThanOrEqual))
                {
                    if (filterValue is DateTime && DateTime.TryParse(filterValue.ToString(), out DateTime value))
                        body += $"{filterFieldName}.Date <= {value.Date}";
                }

                Console.WriteLine(body);

                query = query.WhereDynamic($"x => x.{body}");
            }

            count = query.Count();

            return query;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    private static string BuildDateExpression<K>(object filterValue, string body, string filterFieldName, string equalityType)
    {

        if (DateTime.TryParse(filterValue.ToString(), out DateTime date))
        {
            string formattedDate = date.ToString("yyyy-MM-dd");
            body += $"{filterFieldName}.Date {equalityType} \"{formattedDate}\"";
        }
        return body;
    }
}
