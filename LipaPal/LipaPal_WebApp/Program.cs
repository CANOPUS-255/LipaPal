using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register services (similar to ConfigureServices in Startup.cs)
        builder.Services.AddControllers(); // Add controllers or any other services you need
        builder.Services.AddSwaggerGen(); // Example: Add Swagger support if needed

        var app = builder.Build();

        // Configure the HTTP request pipeline (similar to Configure in Startup.cs)
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run(); // Start the application
    }
}
