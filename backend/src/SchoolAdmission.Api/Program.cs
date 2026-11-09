using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AdmissionDatabase")
    ?? throw new InvalidOperationException(
        "Configure the PostgreSQL connection string with the ConnectionStrings__AdmissionDatabase environment variable.");

builder.Services.AddDbContext<AdmissionDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.Run();
