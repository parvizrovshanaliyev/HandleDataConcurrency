using HandleDataConcurrencies.Data;
using HandleDataConcurrencies.Models;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace HandleDataConcurrencies.Jobs;

public class PaymentProcessorJob : IJob
{
    private readonly PaymentProcessor _paymentProcessor;
    private readonly CancellationToken _cancellationToken;

    public PaymentProcessorJob(PaymentProcessor paymentProcessor, CancellationToken cancellationToken)
    {
        _paymentProcessor = paymentProcessor;
        _cancellationToken = cancellationToken;
    }


    public async Task Execute(IJobExecutionContext context)
    {
        await _paymentProcessor.ProcessPaymentsAsync(_cancellationToken);
    }
}

public class PaymentProcessor
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<PaymentProcessor> _logger;

    public PaymentProcessor(ApplicationDbContext dbContext, ILogger<PaymentProcessor> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ProcessPaymentsAsync(CancellationToken cancellationToken)
    {
        try
        {
            const int MaxPaymentProcessingWaitTimeInMinutes = 3;
            DateTimeOffset now = DateTimeOffset.Now;
            DateTimeOffset timeout = now.AddMinutes(-MaxPaymentProcessingWaitTimeInMinutes);

            var unprocessedPayments = await _dbContext.Payments
                .Where(p => !p.IsProcessed && p.CreateDate <= timeout)
                .Take(50)
                .ToListAsync(cancellationToken);

            _logger.LogInformation($"Found {unprocessedPayments.Count} unprocessed payments.");

            var tasks = unprocessedPayments.Select(p => ProcessPaymentAsync(p, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogInformation("Payment processing completed.");
        }
        catch (OperationCanceledException)
        {
            // The operation was canceled. Do any necessary cleanup here.
            _logger.LogWarning("Payment processing was canceled.");
            throw;
        }
    }

    private async Task ProcessPaymentAsync(Payment payment, CancellationToken cancellationToken)
    {
        // Check if the operation has been canceled before processing the payment
        cancellationToken.ThrowIfCancellationRequested();

        // Get the original value of the RowVersion property so we can avoid concurrency issues
        _dbContext.Entry(payment).Property(p => p.RowVersion).OriginalValue = payment.RowVersion;

        // Mark the payment as processed
        payment.IsProcessed = true;
        payment.Status = PaymentStatus.Processing;

        try
        {
            // Process the payment, for example by making a payment with a bank or confirming an order

            // Save the changes to the database
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Log that the payment was successfully processed
            _logger.LogInformation($"Payment with ID {payment.Id} processed successfully.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // If the payment has already been processed by another transaction, mark it as unprocessed and update the original version of the payment in the database
            var entry = ex.Entries.Single();
            var databaseEntry = await entry.GetDatabaseValuesAsync(cancellationToken);

            if (databaseEntry == null)
            {
                payment.IsProcessed = false;
            }
            else
            {
                var databaseValues = (Payment)databaseEntry.ToObject();

                if (databaseValues.IsProcessed)
                {
                    payment.IsProcessed = false;
                }
                else
                {
                    _dbContext.Entry(payment).State = EntityState.Detached;
                    _dbContext.Entry(databaseValues).State = EntityState.Modified;

                    // Save the changes to the database
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }
}

// public class PaymentProcessingJob : BackgroundService
// {
//     private readonly ILogger<PaymentProcessingJob> _logger;
//     private readonly IServiceProvider _serviceProvider;
//     private readonly int _maxRetryCount = 5;
//     private readonly int[] _fibonacciNumbers = new[] { 1, 1, 2, 3, 5 };
//
//     public PaymentProcessingJob(ILogger<PaymentProcessingJob> logger, IServiceProvider serviceProvider)
//     {
//         _logger = logger;
//         _serviceProvider = serviceProvider;
//     }
//
//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         while (!stoppingToken.IsCancellationRequested)
//         {
//             try
//             {
//                 using var scope = _serviceProvider.CreateScope();
//                 var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//
//                 // Fetch up to 10 waiting payments at a time
//                 var waitingPayments = await dbContext.Payments
//                     .Where(p => p.Status == PaymentStatus.Waiting)
//                     .Take(10)
//                     .ToListAsync(stoppingToken);
//
//                 foreach (var payment in waitingPayments)
//                 {
//                     // Update the status to "Processing"
//                     payment.Status = PaymentStatus.Processing;
//
//                     // Save changes to the database with optimistic concurrency checking
//                     bool saved = false;
//                     int retryCount = 0;
//
//                     while (!saved && retryCount < _maxRetryCount)
//                     {
//                         try
//                         {
//                             await dbContext.SaveChangesAsync(stoppingToken);
//
//                             saved = true;
//                         }
//                         catch (DbUpdateConcurrencyException ex)
//                         {
//                             var entry = ex.Entries.Single();
//                             var databaseValues = await entry.GetDatabaseValuesAsync(stoppingToken);
//
//                             if (databaseValues == null)
//                             {
//                                 _logger.LogInformation(
//                                     $"Payment with id {payment.Id} was deleted by another transaction.");
//                                 break;
//                             }
//                             else
//                             {
//                                 var databasePayment = (Payment)databaseValues.ToObject();
//                                 payment.RowVersion = databasePayment.RowVersion;
//                                 payment.Status = databasePayment.Status;
//                             }
//                         }
//                         catch (Exception ex)
//                         {
//                             _logger.LogError(ex, $"An error occurred while processing payment with id {payment.Id}.");
//                         }
//
//                         if (!saved)
//                         {
//                             // Delay for a number of seconds based on the current retry count
//                             int delay = _fibonacciNumbers[retryCount] * 1000;
//                             await Task.Delay(delay, stoppingToken);
//
//                             retryCount++;
//                         }
//                     }
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "An error occurred while processing payments.");
//             }
//
//             // Delay for 5 minutes before processing more payments
//             await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
//         }
//     }
// }