using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SchoolAdmission.Api.Data;
using Xunit;

namespace SchoolAdmission.Api.Tests;

public sealed class AdmissionClassApiTests
{
    [Fact]
    public async Task PostClasses_ReturnsCreatedClassWithContractFields()
    {
        using var factory = new AdmissionApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/classes",
            new { name = "Class 6 - Morning", totalSeats = 50 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var classJson = document.RootElement;
        Assert.Equal(
            ["id", "name", "totalSeats"],
            classJson.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.StartsWith("c_", classJson.GetProperty("id").GetString());
        Assert.Equal("Class 6 - Morning", classJson.GetProperty("name").GetString());
        Assert.Equal(50, classJson.GetProperty("totalSeats").GetInt32());
        Assert.EndsWith(
            $"/classes/{classJson.GetProperty("id").GetString()}",
            response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task GetClasses_ReturnsSummariesWithContractFields()
    {
        using var factory = new AdmissionApiFactory();
        using var client = factory.CreateClient();
        await CreateClassAsync(client);

        var response = await client.GetAsync("/classes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var classJson = Assert.Single(document.RootElement.EnumerateArray().ToArray());
        Assert.Equal(
            ["availableSeats", "confirmedSeats", "heldSeats", "id", "name", "totalSeats"],
            classJson.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal(50, classJson.GetProperty("totalSeats").GetInt32());
        Assert.Equal(0, classJson.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, classJson.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(50, classJson.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task GetClass_ReturnsSummaryAndNotFoundForUnknownId()
    {
        using var factory = new AdmissionApiFactory();
        using var client = factory.CreateClient();
        var created = await CreateClassAsync(client);
        var classId = created.RootElement.GetProperty("id").GetString();
        created.Dispose();

        var response = await client.GetAsync($"/classes/{classId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            ["availableSeats", "confirmedSeats", "heldSeats", "id", "name", "totalSeats"],
            document.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal(classId, document.RootElement.GetProperty("id").GetString());
        Assert.Equal(0, document.RootElement.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, document.RootElement.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(50, document.RootElement.GetProperty("availableSeats").GetInt32());

        var missingResponse = await client.GetAsync("/classes/c_missing");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task PostClasses_ReturnsBadRequestForInvalidInput()
    {
        using var factory = new AdmissionApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/classes",
            new { name = "Class 6 - Morning", totalSeats = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<JsonDocument> CreateClassAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/classes",
            new { name = "Class 6 - Morning", totalSeats = 50 });
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private sealed class AdmissionApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        public AdmissionApiFactory()
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__AdmissionDatabase",
                "Host=localhost;Database=api_tests;Username=api_tests;Password=api_tests");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:AdmissionDatabase"] =
                        "Host=localhost;Database=api_tests;Username=api_tests;Password=api_tests"
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<AdmissionDbContext>>();
                services.RemoveAll<AdmissionDbContext>();
                services.AddDbContext<AdmissionDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
