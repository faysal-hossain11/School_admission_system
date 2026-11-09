using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Repositories;
using SchoolAdmission.Api.Services;
using SchoolAdmission.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AdmissionDatabase")
    ?? throw new InvalidOperationException(
        "Configure the PostgreSQL connection string with the ConnectionStrings__AdmissionDatabase environment variable.");

builder.Services.AddDbContext<AdmissionDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IAdmissionClassRepository, AdmissionClassRepository>();
builder.Services.AddScoped<IAdmissionHoldService, AdmissionHoldService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(
    new AdmissionHoldOptions(ParseHoldDuration(builder.Configuration["HOLD_DURATION_SECONDS"])));
builder.Services.AddHostedService<AdmissionHoldExpirationWorker>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();

public partial class Program
{
    private static TimeSpan ParseHoldDuration(string? configuredSeconds)
    {
        if (configuredSeconds is null)
        {
            return TimeSpan.FromSeconds(600);
        }

        if (!int.TryParse(configuredSeconds, out var seconds) || seconds <= 0)
        {
            throw new InvalidOperationException(
                "HOLD_DURATION_SECONDS must be a positive whole number of seconds.");
        }

        return TimeSpan.FromSeconds(seconds);
    }
}
