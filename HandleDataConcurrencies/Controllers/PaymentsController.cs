using HandleDataConcurrencies.Data;
using HandleDataConcurrencies.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandleDataConcurrencies.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public PaymentsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
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
        public async Task<IActionResult> Put(int id, [FromBody] Payment payment)
        {
            if (id != payment.Id)
            {
                return BadRequest();
            }

            // remove all ModelState errors
            ModelState.Clear();

            var dbPayment = await _dbContext.Payments.FindAsync(id);
            if (dbPayment == null)
            {
                return NotFound();
            }

            // check for concurrency
            if (!payment.RowVersion.SequenceEqual(dbPayment.RowVersion))
            {
                ModelState.AddModelError("RowVersion", "The payment has been updated or deleted by another user. Please refresh and try again.");
                return Conflict(ModelState);
            }

            dbPayment.Amount = payment.Amount;
            dbPayment.IsProcessed = payment.IsProcessed;
            dbPayment.Status = payment.Status;
            dbPayment.UpdateDate = DateTime.UtcNow;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("RowVersion", "The payment has been updated or deleted by another user. Please refresh and try again.");
                return Conflict(ModelState);
            }

            return NoContent();
        }
    }

}
