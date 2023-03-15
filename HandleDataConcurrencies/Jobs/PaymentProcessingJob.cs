using HandleDataConcurrencies.Data;
using HandleDataConcurrencies.Models;
using Microsoft.EntityFrameworkCore;

namespace HandleDataConcurrencies.Jobs;

public class PaymentProcessingJob : BackgroundService
{
    private readonly ILogger<PaymentProcessingJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly int _maxRetryCount = 5;
    private readonly int[] _fibonacciNumbers = new[] { 1, 1, 2, 3, 5 };

    public PaymentProcessingJob(ILogger<PaymentProcessingJob> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Fetch up to 10 waiting payments at a time
                var waitingPayments = await dbContext.Payments
                    .Where(p => p.Status == PaymentStatus.Waiting)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var payment in waitingPayments)
                {
                    // Update the status to "Processing"
                    payment.Status = PaymentStatus.Processing;

                    // Save changes to the database with optimistic concurrency checking
                    bool saved = false;
                    int retryCount = 0;

                    while (!saved && retryCount < _maxRetryCount)
                    {
                        try
                        {
                            await dbContext.SaveChangesAsync(stoppingToken);

                            saved = true;
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            var entry = ex.Entries.Single();
                            var databaseValues = await entry.GetDatabaseValuesAsync(stoppingToken);

                            if (databaseValues == null)
                            {
                                _logger.LogInformation(
                                    $"Payment with id {payment.Id} was deleted by another transaction.");
                                break;
                            }
                            else
                            {
                                var databasePayment = (Payment)databaseValues.ToObject();
                                payment.RowVersion = databasePayment.RowVersion;
                                payment.Status = databasePayment.Status;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"An error occurred while processing payment with id {payment.Id}.");
                        }

                        if (!saved)
                        {
                            // Delay for a number of seconds based on the current retry count
                            int delay = _fibonacciNumbers[retryCount] * 1000;
                            await Task.Delay(delay, stoppingToken);

                            retryCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing payments.");
            }

            // Delay for 5 minutes before processing more payments
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
