using System.Globalization;
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
using SchoolAdmission.Api.Models;
using Xunit;

namespace SchoolAdmission.Api.Tests;

[CollectionDefinition("Admission API tests", DisableParallelization = true)]
public sealed class AdmissionApiTestCollection
{
}

[Collection("Admission API tests")]
public sealed class AdmissionHoldApiTests
{
    [Fact]
    public async Task HoldCreation_ExpiresAutomaticallyAndReleasesCapacity()
    {
        await using var fixture = await PostgresAdmissionApiFixture.CreateAsync(
            TimeSpan.FromMinutes(10));
        var classId = await fixture.CreateClassAsync(totalSeats: 1);
        using var createResponse = await fixture.CreateHoldAsync(classId, "student-expiry");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var createdDocument = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());
        var holdId = createdDocument.RootElement.GetProperty("holdId").GetString()!;
        var expiresAt = createdDocument.RootElement.GetProperty("expiresAt").GetDateTimeOffset();
        Assert.Equal("HELD", createdDocument.RootElement.GetProperty("status").GetString());
        Assert.True(expiresAt > fixture.Clock.GetUtcNow());

        using var heldResponse = await fixture.Client.GetAsync($"/holds/{holdId}");
        Assert.Equal(HttpStatusCode.OK, heldResponse.StatusCode);
        using var heldDocument = JsonDocument.Parse(await heldResponse.Content.ReadAsStringAsync());
        Assert.Equal("HELD", heldDocument.RootElement.GetProperty("status").GetString());

        fixture.Clock.Advance(TimeSpan.FromMinutes(11));
        await fixture.WaitForPersistedExpirationAsync(holdId);

        using var expiredResponse = await fixture.Client.GetAsync($"/holds/{holdId}");
        Assert.Equal(HttpStatusCode.OK, expiredResponse.StatusCode);
        using var expiredDocument = JsonDocument.Parse(
            await expiredResponse.Content.ReadAsStringAsync());
        Assert.Equal("EXPIRED", expiredDocument.RootElement.GetProperty("status").GetString());

        using var classResponse = await fixture.Client.GetAsync($"/classes/{classId}");
        Assert.Equal(HttpStatusCode.OK, classResponse.StatusCode);
        using var classDocument = JsonDocument.Parse(
            await classResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, classDocument.RootElement.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task Confirmation_IsIdempotentAndEnforcesOwnershipAndDuplicatePrevention()
    {
        await using var fixture = await PostgresAdmissionApiFixture.CreateAsync(
            TimeSpan.FromMinutes(10));
        var classId = await fixture.CreateClassAsync(totalSeats: 2);
        using var createResponse = await fixture.CreateHoldAsync(classId, "student-owner");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createDocument = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());
        var holdId = createDocument.RootElement.GetProperty("holdId").GetString()!;

        using var duplicateWhileHeld = await fixture.CreateHoldAsync(classId, "student-owner");
        Assert.Equal(HttpStatusCode.Conflict, duplicateWhileHeld.StatusCode);

        using var unauthorizedConfirmation = await fixture.ConfirmHoldAsync(
            holdId,
            "different-student");
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedConfirmation.StatusCode);

        using var confirmation = await fixture.ConfirmHoldAsync(holdId, "student-owner");
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        using var repeatedConfirmation = await fixture.ConfirmHoldAsync(holdId, "student-owner");
        Assert.Equal(HttpStatusCode.OK, repeatedConfirmation.StatusCode);

        using var confirmedDuplicate = await fixture.CreateHoldAsync(classId, "student-owner");
        Assert.Equal(HttpStatusCode.Conflict, confirmedDuplicate.StatusCode);

        using var classResponse = await fixture.Client.GetAsync($"/classes/{classId}");
        Assert.Equal(HttpStatusCode.OK, classResponse.StatusCode);
        using var classDocument = JsonDocument.Parse(
            await classResponse.Content.ReadAsStringAsync());
        var summary = classDocument.RootElement;
        var heldSeats = summary.GetProperty("heldSeats").GetInt32();
        var confirmedSeats = summary.GetProperty("confirmedSeats").GetInt32();
        Assert.Equal(0, heldSeats);
        Assert.Equal(1, confirmedSeats);
        Assert.True(heldSeats + confirmedSeats <= summary.GetProperty("totalSeats").GetInt32());
    }

    [Fact]
    public async Task ConcurrentHoldRequests_NeverExceedClassCapacity()
    {
        const int totalSeats = 4;
        const int requestCount = 16;

        await using var fixture = await PostgresAdmissionApiFixture.CreateAsync(
            TimeSpan.FromMinutes(10));
        var classId = await fixture.CreateClassAsync(totalSeats);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, requestCount)
                .Select(index => fixture.CreateHoldAsync(classId, $"concurrent-student-{index}")));
        try
        {
            var unexpectedResponses = responses
                .Where(response =>
                    response.StatusCode != HttpStatusCode.Created &&
                    response.StatusCode != HttpStatusCode.Conflict)
                .ToArray();
            var unexpectedDetails = unexpectedResponses
                .Select(response => $"{(int)response.StatusCode} {response.StatusCode}")
                .ToArray();
            Assert.True(
                unexpectedResponses.Length == 0,
                $"Unexpected concurrent API responses: {string.Join(" | ", unexpectedDetails)}");

            Assert.Equal(
                totalSeats,
                responses.Count(response => response.StatusCode == HttpStatusCode.Created));
            Assert.Equal(
                requestCount - totalSeats,
                responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
            Assert.All(
                responses,
                response => Assert.Contains(
                    response.StatusCode,
                    new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }));

            using var classResponse = await fixture.Client.GetAsync($"/classes/{classId}");
            Assert.Equal(HttpStatusCode.OK, classResponse.StatusCode);
            using var classDocument = JsonDocument.Parse(
                await classResponse.Content.ReadAsStringAsync());
            var summary = classDocument.RootElement;
            var heldSeats = summary.GetProperty("heldSeats").GetInt32();
            var confirmedSeats = summary.GetProperty("confirmedSeats").GetInt32();
            Assert.True(
                heldSeats + confirmedSeats <= totalSeats,
                $"Concurrent requests reserved {heldSeats + confirmedSeats} seats for a capacity of {totalSeats}.");

            var persistedSeatCount = await fixture.GetActiveSeatCountAsync(classId);
            Assert.True(
                persistedSeatCount <= totalSeats,
                $"PostgreSQL contains {persistedSeatCount} active or confirmed holds for a capacity of {totalSeats}.");
            Assert.Equal(totalSeats, persistedSeatCount);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    private sealed class PostgresAdmissionApiFixture : IAsyncDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly List<string> _classIds = [];

        private PostgresAdmissionApiFixture(TimeSpan holdDuration)
        {
            Clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
            _factory = new PostgresAdmissionApiFactory(
                Clock,
                checked((int)holdDuration.TotalSeconds));
            Client = _factory.CreateClient();
        }

        public HttpClient Client { get; }

        public MutableTimeProvider Clock { get; }

        public static async Task<PostgresAdmissionApiFixture> CreateAsync(
            TimeSpan holdDuration)
        {
            var fixture = new PostgresAdmissionApiFixture(holdDuration);
            try
            {
                await using var scope = fixture._factory.Services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
                if (!await dbContext.Database.CanConnectAsync())
                {
                    throw new InvalidOperationException(
                        "The PostgreSQL API test database is unavailable. Configure the backend connection string for a test database before running these tests.");
                }

                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync();
                throw;
            }
        }

        public async Task<string> CreateClassAsync(int totalSeats)
        {
            using var response = await Client.PostAsJsonAsync(
                "/classes",
                new
                {
                    name = $"Automated test {Guid.NewGuid():N}",
                    totalSeats
                });
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var classId = document.RootElement.GetProperty("id").GetString()!;
            _classIds.Add(classId);
            return classId;
        }

        public Task<HttpResponseMessage> CreateHoldAsync(string classId, string studentId) =>
            SendWithStudentHeaderAsync(
                HttpMethod.Post,
                $"/classes/{classId}/holds",
                studentId);

        public Task<HttpResponseMessage> ConfirmHoldAsync(string holdId, string studentId) =>
            SendWithStudentHeaderAsync(
                HttpMethod.Post,
                $"/holds/{holdId}/confirm",
                studentId);

        public async Task WaitForPersistedExpirationAsync(string holdId)
        {
            for (var attempt = 0; attempt < 50; attempt++)
            {
                await using var scope = _factory.Services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
                var status = await dbContext.AdmissionHolds
                    .AsNoTracking()
                    .Where(hold => hold.Id == holdId)
                    .Select(hold => (AdmissionHoldStatus?)hold.Status)
                    .SingleAsync();
                if (status == AdmissionHoldStatus.EXPIRED)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            throw new Xunit.Sdk.XunitException(
                $"The background worker did not persist expiration for hold {holdId}.");
        }

        public async Task<int> GetActiveSeatCountAsync(string classId)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
            return await dbContext.AdmissionHolds.CountAsync(
                hold => hold.AdmissionClassId == classId &&
                        (hold.Status == AdmissionHoldStatus.HELD ||
                         hold.Status == AdmissionHoldStatus.CONFIRMED));
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_classIds.Count > 0)
                {
                    await using var scope = _factory.Services.CreateAsyncScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
                    foreach (var classId in _classIds)
                    {
                        await dbContext.AdmissionHolds
                            .Where(hold => hold.AdmissionClassId == classId)
                            .ExecuteDeleteAsync();
                        await dbContext.AdmissionClasses
                            .Where(admissionClass => admissionClass.Id == classId)
                            .ExecuteDeleteAsync();
                    }
                }
            }
            finally
            {
                Client.Dispose();
                await _factory.DisposeAsync();
            }
        }

        private async Task<HttpResponseMessage> SendWithStudentHeaderAsync(
            HttpMethod method,
            string path,
            string studentId)
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Add("X-Student-Id", studentId);
            return await Client.SendAsync(request);
        }
    }

    private sealed class PostgresAdmissionApiFactory(
        MutableTimeProvider clock,
        int holdDurationSeconds) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["HOLD_DURATION_SECONDS"] =
                        holdDurationSeconds.ToString(CultureInfo.InvariantCulture)
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(clock);
            });
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset initialTime) : TimeProvider
    {
        private long _utcTicks = initialTime.UtcTicks;

        public override DateTimeOffset GetUtcNow() =>
            new(Interlocked.Read(ref _utcTicks), TimeSpan.Zero);

        public void Advance(TimeSpan duration) => Interlocked.Add(ref _utcTicks, duration.Ticks);
    }
}
