using HandleDataConcurrencies.Data;
using HandleDataConcurrencies.Models;
using HandleDataConcurrency.Models;
using HandleDataConcurrency.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandleDataConcurrencies.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IPaymentService _service;

        public PaymentsController(ApplicationDbContext dbContext, IPaymentService service)
        {
            _dbContext = dbContext;
            _service = service;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var payment = await _dbContext.Payments.FindAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            return Ok(payment);
        }

        [HttpPost("GetAll")]
        public async Task<IActionResult> GetAll(PagingRequest request)
        {
            return Ok(await _service.GetAllAsync(request));
        }


        [HttpPost("CreatePayments")]
        public async Task<ActionResult> CreatePayments(int count = 10000)
        {
            var payments = new List<Payment>();

            for (int i = 0; i < count; i++)
            {
                var payment = new Payment
                {
                    Amount = new Random().Next(10, 10000),
                    Status = PaymentStatus.Waiting,
                    CreateDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow
                };

                payments.Add(payment);
            }

            _dbContext.Payments.AddRange(payments);

            await _dbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, decimal? amount, CancellationToken cancellationToken = default(CancellationToken))
        {

            var savedData = false;
            var retryCount = 0;

            while (!savedData && retryCount < 3)
            {
                try
                {
                    var dbPayment =
                        await _dbContext.Payments.FindAsync(new object?[] { id }, cancellationToken: cancellationToken);

                    if (dbPayment == null)
                    {
                        return NotFound();
                    }


                    dbPayment.Amount = amount ?? 100;
                    dbPayment.IsProcessed = true;
                    dbPayment.Status = PaymentStatus.Processing;
                    dbPayment.UpdateDate = DateTime.UtcNow;

                    _dbContext.Payments.Update(dbPayment);
                    // Save the changes to the database
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    savedData = true;
                    retryCount = 3;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await HandleDbUpdateConcurrencyExceptionAsync<Payment>(ex);
                    retryCount++;
                }
            }


            return Ok();
        }
        
        private async Task HandleDbUpdateConcurrencyExceptionAsync<T>(DbUpdateConcurrencyException ex)
            where T : class
        {
            var entry = ex.Entries.FirstOrDefault(e => e.Entity is T);
            if (entry == null)
            {
                throw new NotSupportedException($"Entity of type {typeof(T).Name} not found in {ex.GetType().Name}");
            }

            var currentValues = entry.CurrentValues;
            var dbValues = await entry.GetDatabaseValuesAsync();

            if (dbValues == null)
            {
                throw new NotSupportedException($"Unable to get database values for entity of type {typeof(T).Name}");
            }

            foreach (var prop in currentValues.Properties)
            {
                var currentValue = currentValues[prop];
                var dbValue = dbValues[prop];

                // Detect changes and handle accordingly
                if (!Equals(currentValue, dbValue))
                {
                    entry.OriginalValues[prop] = dbValue;
                }
            }
        }
    }
}