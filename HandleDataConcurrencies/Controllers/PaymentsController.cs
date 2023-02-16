using HandleDataConcurrencies.Data;
using HandleDataConcurrencies.Models;
using Microsoft.AspNetCore.Http;
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
        public async Task<IActionResult> Put(int id, Payment payment)
        {
            if (id != payment.Id)
            {
                return BadRequest();
            }

            _dbContext.Entry(payment).State = EntityState.Modified;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var entry = ex.Entries.Single();
                var clientValues = (Payment)entry.Entity;
                var databaseValues = await entry.GetDatabaseValuesAsync();

                if (databaseValues == null)
                {
                    return NotFound();
                }

                var databasePayment = (Payment)databaseValues.ToObject();
                if (databasePayment.UpdateDate > clientValues.UpdateDate)
                {
                    ModelState.AddModelError("UpdateDate", "The payment has been updated by another user. Please refresh and try again.");
                    return Conflict();
                }
                else
                {
                    ModelState.AddModelError("", "The record you attempted to edit was modified by another user after you got the original value. The edit operation was canceled and the current values in the database have been displayed. If you still want to edit this record, click the Save button again.");
                    payment.RowVersion = databasePayment.RowVersion;
                    ModelState.Remove("RowVersion");
                    return BadRequest(ModelState);
                }
            }

            return NoContent();
        }
    }

}
