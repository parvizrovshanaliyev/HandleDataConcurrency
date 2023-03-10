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
        
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductPutRequest request,CancellationToken cancellationToken)
        {
            
            var existingProduct = await _dbContext.Products.FindAsync(id);

            if (existingProduct == null)
            {
                return NotFound("Product not found.");
            }

            if (!existingProduct.Version.SequenceEqual(request.Version))
            {
                return BadRequest("Version mismatch.");
            }

            existingProduct.Name = request.Name;
            existingProduct.Price = request.Price.Value;
            existingProduct.Version = Guid.NewGuid().ToByteArray();

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Get(id))
                {
                    return NotFound("Product not found.");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // // PUT: api/Products/5
        // [HttpPut("PutWithRetry/{id}")]
        // public async Task<IActionResult> PutWithRetry(
        //     int id,
        //     [FromBody] ProductPutRequest request,
        //     CancellationToken cancellationToken
        // )
        // {
        //     var retryCount = 3; // maximum number of retries
        //     var retryDelay = TimeSpan.FromSeconds(1); // delay between retries
        //     var succeeded = false;
        //
        //     while (!succeeded && retryCount > 0)
        //     {
        //         var product = await _dbContext.Products
        //             .FirstOrDefaultAsync(p => p.Id == id,
        //                 cancellationToken: cancellationToken);
        //
        //         if (product == null)
        //         {
        //             // product not found
        //             return NotFound();
        //         }
        //
        //         request.Name ??= "Updated Name";
        //         request.Description ??= "Updated Description";
        //         request.Price ??= 99.99m;
        //         // update product
        //         product.Name = request.Name;
        //         product.Description = request.Description;
        //         product.Price = request.Price.Value;
        //
        //         // check version
        //         var entry = _dbContext.Entry(product);
        //         if (entry.State == EntityState.Modified)
        //         {
        //             // optimistic concurrency check
        //             var currentVersion = (int)entry.OriginalValues[nameof(Product.Version)];
        //             var newVersion = currentVersion + 1;
        //             entry.CurrentValues[nameof(Product.Version)] = newVersion;
        //             entry.OriginalValues[nameof(Product.Version)] = currentVersion;
        //
        //             // save changes
        //             try
        //             {
        //                 await _dbContext.SaveChangesAsync(cancellationToken);
        //                 // success
        //                 succeeded = true;
        //             }
        //             catch (DbUpdateConcurrencyException ex)
        //             {
        //                 // concurrency conflict
        //                 var databaseValues = await ex.Entries.Single().GetDatabaseValuesAsync(cancellationToken);
        //                 if (databaseValues == null)
        //                 {
        //                     // the entity was deleted
        //                     return BadRequest();
        //                 }
        //
        //                 var databaseVersion = (int)databaseValues[nameof(Product.Version)];
        //                 if (databaseVersion != currentVersion)
        //                 {
        //                     // the entity was updated by another user
        //                     // the entity was deleted
        //                     return BadRequest();
        //                 }
        //
        //                 // retry the update
        //                 retryCount--;
        //                 await Task.Delay(retryDelay, cancellationToken);
        //             }
        //         }
        //     }
        //
        //     if (!succeeded)
        //     {
        //         // exceeded maximum number of retries
        //         return StatusCode((int)HttpStatusCode.ServiceUnavailable);
        //     }
        //
        //     return Ok();
        // }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(
            int id,
            [FromBody] ProductPutRequest request,
            CancellationToken cancellationToken
        )
        {
            var product = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == id,
                    cancellationToken: cancellationToken);

            if (product == null)
            {
                // product not found
                return NotFound();
            }

            request.Name ??= "Updated Name";
            request.Description ??= "Updated Description";
            request.Price ??= 99.99m;
            // update product
            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price.Value;

            // check version
            var entry = _dbContext.Entry(product);
            if (entry.State == EntityState.Modified)
            {
                // optimistic concurrency check
                var currentVersion = (int)entry.OriginalValues[nameof(Product.Version)];
                var newVersion = currentVersion + 1;
                entry.CurrentValues[nameof(Product.Version)] = newVersion;
                entry.OriginalValues[nameof(Product.Version)] = currentVersion;

                // save changes
                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // concurrency conflict
                    var databaseValues = await ex.Entries.Single().GetDatabaseValuesAsync(cancellationToken);
                    if (databaseValues == null)
                    {
                        // the entity was deleted
                        return BadRequest();
                    }

                    var databaseVersion = (int)databaseValues[nameof(Product.Version)];
                    if (databaseVersion != currentVersion)
                    {
                        // the entity was updated by another user
                        // the entity was deleted
                        return BadRequest();
                    }
                }
            }

            return Ok();
        }
    }
}