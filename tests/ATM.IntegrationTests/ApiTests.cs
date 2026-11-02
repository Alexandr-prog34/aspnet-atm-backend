using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Dapper;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;
using Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace ATM.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("atm")
        .WithUsername("atm")
        .WithPassword("integration-test-password")
        .Build();

    public AtmFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Factory = new AtmFactory(ConnectionString);
        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
        await _postgres.DisposeAsync();
    }
}

public sealed class AtmFactory(string connectionString) : WebApplicationFactory<Program>
{
    private static readonly RSA Rsa = RSA.Create(2048);
    private static readonly RsaSecurityKey SigningKey = new(Rsa) { KeyId = "integration-key" };
    private const string Issuer = "http://localhost:8081/realms/atm";

    public static string Token(string subject, string role, string? issuer = null, string? audience = null,
        DateTime? expires = null, bool invalidSignature = false)
    {
        SigningCredentials credentials = invalidSignature
            ? new SigningCredentials(new RsaSecurityKey(RSA.Create(2048)), SecurityAlgorithms.RsaSha256)
            : new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256);
        var jwt = new JwtSecurityToken(issuer ?? Issuer, audience ?? "atm-api",
            [new Claim("sub", subject), new Claim("realm_access", JsonSerializer.Serialize(new { roles = new[] { role } }))],
            expires: expires ?? DateTime.UtcNow.AddMinutes(15), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Atm"] = connectionString }));
        builder.ConfigureTestServices(services => services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var discovery = new OpenIdConnectConfiguration { Issuer = Issuer };
                discovery.SigningKeys.Add(SigningKey);
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(discovery);
            }));
        builder.UseEnvironment("Development");
    }
}

public sealed class LegacyDatabaseMigrationTests
{
    [Fact]
    public async Task ExistingAccountSurvivesMigrationAndOldSessionsAreRemoved()
    {
        await using PostgreSqlContainer postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("atm")
            .WithUsername("atm")
            .WithPassword("legacy-test-password")
            .Build();
        await postgres.StartAsync();
        Guid accountId = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(postgres.GetConnectionString()))
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync("""
                CREATE TABLE accounts (
                    id uuid PRIMARY KEY,
                    number varchar(64) NOT NULL,
                    pin_hash varchar(200) NOT NULL,
                    balance numeric(18,2) NOT NULL,
                    created_at_utc timestamptz NOT NULL
                );
                CREATE UNIQUE INDEX "IX_accounts_number" ON accounts (number);
                CREATE TABLE sessions (id uuid PRIMARY KEY);
                INSERT INTO accounts (id, number, pin_hash, balance, created_at_utc)
                VALUES (@Id, 'legacy-account', 'legacy-hash', 42.50, @CreatedAtUtc);
                """, new { Id = accountId, CreatedAtUtc = DateTime.UtcNow });
        }

        using var factory = new AtmFactory(postgres.GetConnectionString());
        using HttpClient client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        await using var verification = new NpgsqlConnection(postgres.GetConnectionString());
        await verification.OpenAsync();
        Assert.Equal(42.50m, await verification.QuerySingleAsync<decimal>(
            "SELECT balance FROM accounts WHERE id = @Id", new { Id = accountId }));
        Assert.Null(await verification.QuerySingleAsync<string?>(
            "SELECT owner_sub FROM accounts WHERE id = @Id", new { Id = accountId }));
        Assert.False(await verification.ExecuteScalarAsync<bool>(
            "SELECT to_regclass('public.sessions') IS NOT NULL"));
    }
}

public sealed class ApiTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string Admin = "admin-sub";

    [Fact]
    public async Task JwtSignatureIssuerAudienceExpiryAndRolesAreEnforced()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Client.GetAsync("/api/accounts/me/balance")).StatusCode);
        foreach (string token in new[]
        {
            AtmFactory.Token("x", "user", issuer: "http://evil/realms/atm"),
            AtmFactory.Token("x", "user", audience: "other-api"),
            AtmFactory.Token("x", "user", expires: DateTime.UtcNow.AddMinutes(-10)),
            AtmFactory.Token("x", "user", invalidSignature: true),
        })
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await SendAsync(HttpMethod.Get, "/api/accounts/me/balance", null, token)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await SendAsync(HttpMethod.Get, "/api/accounts/me/balance", null,
                AtmFactory.Token(Admin, "admin"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await SendAsync(HttpMethod.Post, "/api/accounts", new
            {
                accountNumber = NewNumber(),
                ownerSubject = "x",
                pin = "1234",
                initialBalance = 0
            }, AtmFactory.Token("x", "user"))).StatusCode);
    }

    [Fact]
    public async Task MigrationsAccountCreationAndOwnershipWork()
    {
        await using var db = new NpgsqlConnection(fixture.ConnectionString);
        await db.OpenAsync();
        Assert.True(await db.ExecuteScalarAsync<bool>("SELECT to_regclass('public.accounts') IS NOT NULL"));
        Assert.False(await db.ExecuteScalarAsync<bool>("SELECT to_regclass('public.sessions') IS NOT NULL"));

        string owner = NewNumber();
        string number = NewNumber();
        await CreateAccountAsync(owner, number, 50m);
        Assert.Equal(50m, await BalanceAsync(owner));
        Assert.Equal(HttpStatusCode.NotFound,
            (await SendAsync(HttpMethod.Get, "/api/accounts/me/balance", null,
                AtmFactory.Token(NewNumber(), "user"))).StatusCode);
        HttpResponseMessage duplicate = await SendAsync(HttpMethod.Post, "/api/accounts",
            new { accountNumber = number, ownerSubject = NewNumber(), pin = "1234", initialBalance = 0m },
            AtmFactory.Token(Admin, "admin"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(1, await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM financial_transactions WHERE type = 'opening' AND account_id = (SELECT id FROM accounts WHERE owner_sub = @Owner)",
            new { Owner = owner }));
    }

    [Fact]
    public async Task DepositWithdrawalTransferIdempotencyAndHistoryWork()
    {
        string source = NewNumber();
        string target = NewNumber();
        string targetNumber = NewNumber();
        await CreateAccountAsync(source, NewNumber(), 100m);
        await CreateAccountAsync(target, targetNumber, 0m);
        string key = NewNumber();
        HttpResponseMessage deposit = await UserPostAsync(source, "deposits", new { amount = 25m }, key);
        Assert.Equal(HttpStatusCode.OK, deposit.StatusCode);
        MoneyOperationResponse original = (await deposit.Content.ReadFromJsonAsync<MoneyOperationResponse>(Json))!;
        HttpResponseMessage replay = await UserPostAsync(source, "deposits", new { amount = 25m }, key);
        Assert.Equal(original, await replay.Content.ReadFromJsonAsync<MoneyOperationResponse>(Json));
        Assert.Equal(HttpStatusCode.Conflict,
            (await UserPostAsync(source, "deposits", new { amount = 30m }, key)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await UserPostAsync(source, "withdrawals", new { amount = 10m, pin = "bad" }, NewNumber())).StatusCode);
        string withdrawalKey = NewNumber();
        Assert.Equal(HttpStatusCode.OK,
            (await UserPostAsync(source, "withdrawals", new { amount = 10m, pin = "1234" }, withdrawalKey)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await UserPostAsync(source, "withdrawals", new { amount = 11m, pin = "bad" }, withdrawalKey)).StatusCode);
        HttpResponseMessage transfer = await UserPostAsync(source, "transfers",
            new { targetAccountNumber = targetNumber, amount = 15m, pin = "1234" }, NewNumber());
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);
        MoneyOperationResponse transferResult = (await transfer.Content.ReadFromJsonAsync<MoneyOperationResponse>(Json))!;
        Assert.Equal(100m, await BalanceAsync(source));
        Assert.Equal(15m, await BalanceAsync(target));
        var sourceHistory = await HistoryAsync(source);
        var targetHistory = await HistoryAsync(target);
        Assert.Equal(4, sourceHistory.Length);
        Assert.Single(targetHistory);
        Assert.Contains(sourceHistory, x => x.OperationId == transferResult.OperationId && x.Type == "TransferOut");
        Assert.Equal(transferResult.OperationId, targetHistory[0].OperationId);
    }

    [Fact]
    public async Task ConcurrentWithdrawalsCannotOverdraw()
    {
        string owner = NewNumber();
        await CreateAccountAsync(owner, NewNumber(), 1000m);
        Task<HttpResponseMessage>[] requests = Enumerable.Range(0, 2)
            .Select(_ => UserPostAsync(owner, "withdrawals", new { amount = 800m, pin = "1234" }, NewNumber()))
            .ToArray();
        HttpResponseMessage[] responses = await Task.WhenAll(requests);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(200m, await BalanceAsync(owner));
        Assert.Equal(2, (await HistoryAsync(owner)).Length);
    }

    [Fact]
    public async Task PinLockPersistsAndIsNotStoredInHistoryOrIdempotency()
    {
        string owner = NewNumber();
        await CreateAccountAsync(owner, NewNumber(), 100m);
        for (int i = 0; i < 4; i++)
            Assert.Equal(HttpStatusCode.Forbidden,
                (await UserPostAsync(owner, "withdrawals", new { amount = 1m, pin = "secret-wrong" }, NewNumber())).StatusCode);
        Assert.Equal((HttpStatusCode)429,
            (await UserPostAsync(owner, "withdrawals", new { amount = 1m, pin = "secret-wrong" }, NewNumber())).StatusCode);
        Assert.Equal((HttpStatusCode)429,
            (await UserPostAsync(owner, "withdrawals", new { amount = 1m, pin = "1234" }, NewNumber())).StatusCode);
        Assert.Equal(100m, await BalanceAsync(owner));
        await using var db = new NpgsqlConnection(fixture.ConnectionString);
        await db.OpenAsync();
        Assert.Equal(5, await db.ExecuteScalarAsync<int>(
            "SELECT failed_pin_attempts FROM accounts WHERE owner_sub = @Owner", new { Owner = owner }));
        Assert.Equal(0, await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM idempotency_keys WHERE account_id = (SELECT id FROM accounts WHERE owner_sub = @Owner)",
            new { Owner = owner }));
    }

    [Fact]
    public async Task SameKeyConcurrentDepositCreditsOnce()
    {
        string owner = NewNumber();
        await CreateAccountAsync(owner, NewNumber(), 0m);
        string key = NewNumber();
        HttpResponseMessage[] responses = await Task.WhenAll(
            UserPostAsync(owner, "deposits", new { amount = 80m }, key),
            UserPostAsync(owner, "deposits", new { amount = 80m }, key));
        Assert.All(responses, x => Assert.Equal(HttpStatusCode.OK, x.StatusCode));
        Assert.Equal(80m, await BalanceAsync(owner));
        Assert.Single(await HistoryAsync(owner));
        using var anotherFactory = new AtmFactory(fixture.ConnectionString);
        using HttpClient anotherClient = anotherFactory.CreateClient();
        Assert.Equal(HttpStatusCode.OK,
            (await UserPostAsync(owner, "deposits", new { amount = 80m }, key, anotherClient)).StatusCode);
        Assert.Equal(80m, await BalanceAsync(owner));
    }

    [Fact]
    public async Task DatabaseErrorRollsBackBalanceAndHistory()
    {
        string owner = NewNumber();
        Guid accountId = await CreateAccountAsync(owner, NewNumber(), 100m);
        await using var db = new NpgsqlConnection(fixture.ConnectionString);
        await db.OpenAsync();
        Guid openingId = await db.QuerySingleAsync<Guid>(
            "SELECT id FROM financial_transactions WHERE account_id = @AccountId", new { AccountId = accountId });
        using IServiceScope scope = fixture.Factory.Services.CreateScope();
        IFinancialTransactionRunner runner = scope.ServiceProvider.GetRequiredService<IFinancialTransactionRunner>();
        await Assert.ThrowsAsync<PostgresException>(() => runner.ExecuteAsync(async (tx, ct) =>
        {
            Account account = (await tx.LockAccountAsync(accountId, ct))!;
            account.Deposit(10m);
            tx.SaveAccount(account);
            tx.AddTransaction(new Transaction(openingId, Guid.NewGuid(), accountId, account.Number,
                new TransactionType.Deposit(), new Money(10m), DateTime.UtcNow, account.Balance));
            return 0;
        }));
        Assert.Equal(100m, await BalanceAsync(owner));
        Assert.Single(await HistoryAsync(owner));
    }

    private async Task<Guid> CreateAccountAsync(string owner, string number, decimal initialBalance)
    {
        HttpResponseMessage response = await SendAsync(HttpMethod.Post, "/api/accounts",
            new { accountNumber = number, ownerSubject = owner, pin = "1234", initialBalance },
            AtmFactory.Token(Admin, "admin"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateAccountResponse>(Json))!.AccountKey;
    }

    private async Task<decimal> BalanceAsync(string owner)
    {
        HttpResponseMessage response = await SendAsync(HttpMethod.Get, "/api/accounts/me/balance", null,
            AtmFactory.Token(owner, "user"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BalanceResponse>(Json))!.Balance;
    }

    private async Task<TransactionHistoryItemResponse[]> HistoryAsync(string owner)
    {
        HttpResponseMessage response = await SendAsync(HttpMethod.Get, "/api/accounts/me/transactions", null,
            AtmFactory.Token(owner, "user"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TransactionHistoryItemResponse[]>(Json))!;
    }

    private Task<HttpResponseMessage> UserPostAsync(string owner, string operation, object body, string key,
        HttpClient? client = null) => SendAsync(HttpMethod.Post, "/api/accounts/me/" + operation,
            body, AtmFactory.Token(owner, "user"), key, client);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body,
        string token, string? key = null, HttpClient? client = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (key is not null) request.Headers.Add("Idempotency-Key", key);
        return await (client ?? fixture.Client).SendAsync(request);
    }

    private static string NewNumber() => Guid.NewGuid().ToString("N");
}
