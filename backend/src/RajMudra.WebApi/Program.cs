using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using RajMudra.Infrastructure;
using RajMudra.Infrastructure.Persistence;
using RajMudra.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);

// CORS for Angular frontend
const string CorsPolicyName = "FrontendCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// JWT authentication & authorization (WebApi layer)
var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection["Issuer"] ?? "RajMudra";
var audience = jwtSection["Audience"] ?? "RajMudraClient";
var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("Missing JWT Secret.");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure PostgreSQL database + tables exist (dev convenience)
await EnsureDatabaseAndSchemaAsync(app.Services, builder.Configuration, app.Environment);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task EnsureDatabaseAndSchemaAsync(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return;
    }

    var csb = new NpgsqlConnectionStringBuilder(connectionString);
    var targetDatabase = string.IsNullOrWhiteSpace(csb.Database) ? "rajmudra" : csb.Database;

    // Connect to maintenance DB to create target DB if needed
    var maintenanceCsb = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Database = "postgres"
    };

    await using (var conn = new NpgsqlConnection(maintenanceCsb.ConnectionString))
    {
        await conn.OpenAsync();

        // In Development, drop and recreate the database so schema matches EF (lowercase columns)
        if (environment.IsDevelopment())
        {
            await using var terminateCmd = new NpgsqlCommand(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @db AND pid <> pg_backend_pid()", conn);
            terminateCmd.Parameters.AddWithValue("db", targetDatabase);
            await terminateCmd.ExecuteNonQueryAsync();

            await using var dropCmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{targetDatabase}\"", conn);
            await dropCmd.ExecuteNonQueryAsync();
        }

        await using var existsCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @db", conn);
        existsCmd.Parameters.AddWithValue("db", targetDatabase);
        var exists = await existsCmd.ExecuteScalarAsync();

        if (exists is null)
        {
            await using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{targetDatabase}\"", conn);
            await createCmd.ExecuteNonQueryAsync();
        }
    }

    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RajMudraDbContext>();
    await db.Database.EnsureCreatedAsync();
}
