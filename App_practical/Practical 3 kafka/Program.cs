using Calculator.Data;
using Calculator.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Confluent.Kafka;




var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHostedService<KafkaConsumerService>();
builder.Services.AddSingleton<KafkaProducerHandler>();
builder.Services.AddSingleton<KafkaProducerService<Null,string>>();
// builder.Services.AddSwaggerGen();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<CalculatorContext>(
    options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);


builder.Services.AddScoped<ICalculatorService, CalculatorService>();

var app = builder.Build();

async Task WaitForDatabase(CalculatorContext context)
{
    for (int i = 0; i < 10; i++)
    {
        try
        {
            if (await context.Database.CanConnectAsync())
            {
                Console.WriteLine("✅ Database connected!");
                return;
            }
        }
        catch
        {
            Console.WriteLine($"⏳ Waiting for database... ({i + 1}/10)");
            await Task.Delay(3000);
        }
    }
    throw new Exception("Database not available");
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CalculatorContext>();

    // Ждем БД
    await WaitForDatabase(context);

    // Автоматически создает таблицы на основе моделей
    await context.Database.EnsureCreatedAsync();
    Console.WriteLine("✅ Database tables created!");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapControllers();
app.Run();
