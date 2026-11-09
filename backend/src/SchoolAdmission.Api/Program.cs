using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AdmissionDatabase")
    ?? throw new InvalidOperationException(
        "Configure the PostgreSQL connection string with the ConnectionStrings__AdmissionDatabase environment variable.");

builder.Services.AddDbContext<AdmissionDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IAdmissionClassRepository, AdmissionClassRepository>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();

public partial class Program
{
}
