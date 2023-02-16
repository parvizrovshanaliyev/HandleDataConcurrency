using HandleDataConcurrencies.Data;
using HandleDataConcurrencies.Jobs;
using HandleDataConcurrencies.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HandleDataConcurrency.Test;

public class PaymentProcessorTests
{
    private readonly ILogger<PaymentProcessor> _logger = new NullLogger<PaymentProcessor>();
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public PaymentProcessorTests()
    {
        // Use the same database for all three applications
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=HandleDataConcurrency;Trusted_Connection=True;MultipleActiveResultSets=true;Application Name=HandleDataConcurrency;";
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;
    }

    [Fact]
    public async Task ProcessPaymentsAsync_UnprocessedPaymentsExist_ProcessesAllPayments()
    {
        // Arrange
        await using var context = new ApplicationDbContext(_options);
        var paymentProcessor = new PaymentProcessor(context, _logger);
        var cancellationTokenSource = new CancellationTokenSource();
        // Act
        
        await paymentProcessor.ProcessPaymentsAsync(cancellationTokenSource.Token);

        // Assert
        var processedPayments = await context.Payments.Where(p => p.IsProcessed).ToListAsync();
        Assert.Equal(10000, processedPayments.Count);
    }
}

