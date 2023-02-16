using HandleDataConcurrencies.Data;
using HandleDataConcurrencies.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Quartz;
using Quartz.Impl;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddLogging();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);

    // In development, we may be able to log additional info that can help us
    options.EnableDetailedErrors(); // To get field-level error details
    options.EnableSensitiveDataLogging(); // To get parameter values - don`t this in production
    options.ConfigureWarnings(warningAction =>
    {
        warningAction.Log(new EventId[]
        {
            CoreEventId.FirstWithoutOrderByAndFilterWarning,
            CoreEventId.RowLimitingOperationWithoutOrderByWarning
        });
    });
});


//// Register the PaymentProcessor and PaymentProcessorJob dependencies
//builder.Services.AddScoped<PaymentProcessor>();
//builder.Services.AddScoped<PaymentProcessorJob>();

//// Register the Quartz scheduler and the PaymentProcessorJob with the scheduler
//builder.Services.AddSingleton(provider =>
//{
//    var cancellationTokenSource = new CancellationTokenSource();
//    var cancellationToken = cancellationTokenSource.Token;

//    var schedulerFactory = new StdSchedulerFactory();
//    var scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();

//    var job = JobBuilder.Create<PaymentProcessorJob>()
//        .WithIdentity("PaymentProcessorJob")
//        .UsingJobData("cancellationToken", cancellationToken.ToString())
//        .Build();

//    var trigger = TriggerBuilder.Create()
//        .WithIdentity("PaymentProcessorJobTrigger")
//        .StartNow()
//        .WithSimpleSchedule(x => x
//            .WithIntervalInMinutes(2)
//            .RepeatForever())
//        .Build();

//    scheduler.ScheduleJob(job, trigger).GetAwaiter().GetResult();

//    return scheduler;
//});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateTime.Now.AddDays(index),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.MapDefaultControllerRoute();

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}