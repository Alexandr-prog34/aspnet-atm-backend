using System.Security.Claims;
using System.Text.Json;
using FluentMigrator.Runner;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Extensions;
using Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.Extensions;
using Itmo.ObjectOrientedProgramming.Lab5.WebApi.Health;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IConfiguration configuration = builder.Configuration;
string realm = configuration["KEYCLOAK_REALM"] ?? "atm";
string publicBase = (configuration["KEYCLOAK_PUBLIC_URL"] ?? "http://localhost:8081").TrimEnd('/');
string internalBase = (configuration["KEYCLOAK_INTERNAL_URL"] ?? publicBase).TrimEnd('/');
string audience = configuration["KEYCLOAK_CLIENT_ID"] ?? "atm-api";
string issuer = $"{publicBase}/realms/{realm}";

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = issuer;
        options.MetadataAddress = $"{internalBase}/realms/{realm}/.well-known/openid-configuration";
        options.Audience = audience;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = ClaimTypes.Role,
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                ClaimsIdentity? identity = context.Principal?.Identity as ClaimsIdentity;
                if (identity?.FindFirst("sub")?.Value is not { Length: > 0 })
                {
                    context.Fail("Missing subject claim.");
                    return Task.CompletedTask;
                }

                string? realmAccess = identity.FindFirst("realm_access")?.Value;
                if (realmAccess is null) return Task.CompletedTask;
                try
                {
                    using JsonDocument document = JsonDocument.Parse(realmAccess);
                    if (document.RootElement.TryGetProperty("roles", out JsonElement roles) &&
                        roles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement role in roles.EnumerateArray())
                        {
                            if (role.ValueKind == JsonValueKind.String && role.GetString() is { } value)
                                identity.AddClaim(new Claim(ClaimTypes.Role, value));
                        }
                    }
                }
                catch (JsonException)
                {
                    context.Fail("Invalid realm roles claim.");
                }

                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgresql", tags: ["ready"]);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{issuer}/protocol/openid-connect/auth"),
                TokenUrl = new Uri($"{issuer}/protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string> { ["openid"] = "OpenID Connect" },
            },
        },
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("oauth2", document)] = ["openid"],
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(provider => BuildConnectionString(provider.GetRequiredService<IConfiguration>()));

WebApplication app = builder.Build();
using (IServiceScope scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId(audience);
        options.OAuthUsePkce();
        options.OAuthScopes("openid");
    });
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
app.Run();

static string BuildConnectionString(IConfiguration configuration) =>
    configuration.GetConnectionString("Atm") ?? new NpgsqlConnectionStringBuilder
    {
        Host = configuration["DB_HOST"] ?? "localhost",
        Database = configuration["DB_NAME"] ?? "atm",
        Username = configuration["DB_USER"] ?? "atm",
        Password = configuration["DB_PASSWORD"] ?? throw new InvalidOperationException("DB_PASSWORD is required."),
    }.ConnectionString;

public partial class Program;
