using System.Diagnostics;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace HandleDataConcurrency.ApiTests.Extensions;

internal static class HttpResponseMessageExtensions
{
    public static async Task<T?> ReadAndAssertSuccessAsync<T>(this HttpResponseMessage response) where T : class
    {
        response.IsSuccessStatusCode.Should().BeTrue();
        var json = await response.Content.ReadAsStringAsync();
        if (typeof(T) == typeof(string))
        {
            return json as T;
        }
        else
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
        
    public static async Task<Envelope> ReadAndAssertError(this HttpResponseMessage response, HttpStatusCode statusCode)
    {
        response.StatusCode.Should().Be(statusCode);
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<Envelope>(json)!;
    }
    
    public class Envelope
    {
        public int Status { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
        public string? TraceId { get; set; }

        protected Envelope(int status, string? errorMessage, DateTime timestamp, string? traceId)
        {
            Status = status;
            ErrorMessage = errorMessage;
            Timestamp = timestamp;
            TraceId = traceId;
        }

        protected Envelope()
        {

        }

        public static Envelope Create(string error, HttpStatusCode statusCode)
        {
            return new Envelope((int)statusCode, error, DateTime.UtcNow, Activity.Current?.Id);
        }

        public EnvelopeObjectResult ToActionResult()
        {
            return new EnvelopeObjectResult(this);
        }
    }

    public class CreatedResultEnvelope
    {
        public Guid Id { get; set; }

        public CreatedResultEnvelope(Guid id)
        {
            Id = id;
        }
    }
    
    public class EnvelopeObjectResult : ObjectResult
    {
        public EnvelopeObjectResult(Envelope envelope)
            : base(envelope)
        {
            StatusCode = envelope.Status;
        }
    }
}