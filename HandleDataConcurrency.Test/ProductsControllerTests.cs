//using HandleDataConcurrencies.Controllers;
//using HandleDataConcurrencies.Data;
//using HandleDataConcurrency.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Assert = Xunit.Assert;

//namespace HandleDataConcurrency.Test;

//public class ProductsControllerTests
//{
//    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
//    public ProductsControllerTests()
//    {
//        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
//            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HandleDataConcurrency;Trusted_Connection=True;MultipleActiveResultSets=true;Application Name=HandleDataConcurrency;")
//            .Options;
//    }

//    [Fact]
//    public async Task Put_UpdatesProduct()
//    {
//        // Arrange
//        await using var dbContext = new ApplicationDbContext(_dbContextOptions);
//        var controller = new ProductsController(dbContext);

//        var product = new Product
//        {
//            Name = "Test Product",
//            Description = "Test Description",
//            Price = 9.99m,
//        };

//        dbContext.Products.Add(product);
//        await dbContext.SaveChangesAsync();

//        var request = new ProductPutRequest
//        {
//            Name = "New Product Name",
//            Description = "New Product Description",
//            Price = 19.99m,
//        };

//        // Act
//        var result = await controller.Put(product.Id, request, CancellationToken.None);

//        // Assert
//        Assert.IsType<OkResult>(result);

//        var updatedProduct = await dbContext.Products.FindAsync(product.Id);

//        Assert.Equal(request.Name, updatedProduct.Name);
//        Assert.Equal(request.Description, updatedProduct.Description);
//        Assert.Equal(request.Price.Value, updatedProduct.Price);
//    }

//    [Fact]
//    public async Task Put_ConcurrencyHandling()
//    {
//        try
//        {
//            // Arrange
//            await using var dbContext = new ApplicationDbContext(_dbContextOptions);
//            var controller = new ProductsController(dbContext);

//            var product = new Product
//            {
//                Name = "Test Product",
//                Description = "Test Description",
//                Price = 9.99m,
//            };

//            dbContext.Products.Add(product);
//            await dbContext.SaveChangesAsync();

//            var tasks = new List<Task<IActionResult>>();
//            var results = new Dictionary<int, IActionResult>();

//            // Act
//            for (var i = 1; i <= 10; i++)
//            {
//                var request = new ProductPutRequest
//                {
//                    Name = $"Concurrent Product Name {i}",
//                    Description = $"Concurrent Product Description {i}",
//                    Price = 29.99m + i,
//                };
//                // Create a new instance of the DbContext for each task
//                var context = new ApplicationDbContext(_dbContextOptions);

//                // Create a new instance of the controller for each task
//                var prController = new ProductsController(context);
//                var task = prController.Put(product.Id, request, CancellationToken.None);
//                tasks.Add(task);
//            }

//            // Assert
//            await Task.WhenAll(tasks);

//            for (var i = 0; i < tasks.Count; i++)
//            {
//                var result = await tasks[i];
//                if (result is BadRequestResult)
//                {
//                    Console.WriteLine($"Concurrency conflict detected for task {i + 1}");
//                }

//                Assert.True(result is OkResult or BadRequestResult);

//                results.Add(i + 1, result);
//            }

//            Assert.True(results.Count(r => r.Value is OkResult) == 1);
//        }
//        catch (Exception e)
//        {
//            Console.WriteLine(e);
//            throw;
//        }
//    }

//    public static class ScopeUtil
//    {
//        public static async Task Do<T>(IServiceScopeFactory serviceScope,
//            Func<T, Task> work)
//        {
//            using var scope = serviceScope.CreateScope();
//            T tVar = scope.ServiceProvider.GetRequiredService<T>();
//            await work(tVar);
//        }
//    }
//}


