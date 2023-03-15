using HandleDataConcurrencies.Data;
using HandleDataConcurrencies.Models;
using HandleDataConcurrency.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace HandleDataConcurrencies.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public ProductsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        // GET: api/Products
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellation)
        {
            return Ok(await _dbContext.Products.ToListAsync(cancellation));
        }


        // GET: api/Products/5
        [HttpGet("{id}", Name = "Get")]
        public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
        {
            var product = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken: cancellationToken);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        // POST: api/Products
        [HttpPost]
        public async Task<IActionResult> Post(CancellationToken cancellationToken)
        {
            var products = new List<Product>();

            for (int i = 1; i <= 100; i++)
            {
                var product = new Product
                {
                    Name = $"Product {i}",
                    Description = $"Description {i}",
                    Price = 10.99M,
                    CreatedDate = DateTime.UtcNow,
                    //Version = 0
                };

                products.Add(product);
            }

            _dbContext.Products.AddRange(products);

            var saveChangesResult = await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(saveChangesResult);
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(
            int id,
            string name,
            string description,
            decimal? price,
            CancellationToken cancellationToken
        )
        {
            var product = await _dbContext.Products.FindAsync(id, cancellationToken);
            if (product == null)
            {
                return NotFound();
            }

            bool isModified = false;
            if (name != null)
            {
                product.Name = name;
                isModified = true;
            }

            if (description != null)
            {
                product.Description = description;
                isModified = true;
            }

            if (price != null)
            {
                product.Price = price.Value;
                isModified = true;
            }

            if (isModified)
            {
                var entry = _dbContext.Entry(product);
                var currentVersion = (int)entry.OriginalValues[nameof(Product.Version)];
                var newVersion = currentVersion + 1;

                entry.CurrentValues[nameof(Product.Version)] = newVersion;
                entry.OriginalValues[nameof(Product.Version)] = currentVersion;

                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    var databaseValues = await ex.Entries.Single().GetDatabaseValuesAsync(cancellationToken);
                    if (databaseValues == null)
                    {
                        return Conflict();
                    }

                    var databaseVersion = (int)databaseValues[nameof(Product.Version)];
                    if (databaseVersion != currentVersion)
                    {
                        return Conflict();
                    }
                }
            }

            return Ok();
        }

    }
}