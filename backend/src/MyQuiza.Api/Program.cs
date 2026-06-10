using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Auth;
using MyQuiza.Api.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Maps onto EduBridge's existing Supabase Postgres. snake_case columns/tables.
// Connection string comes from env/user-secrets (ConnectionStrings__DefaultConnection).
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
       .UseSnakeCaseNamingConvention());

// Validate Supabase-issued JWTs + register Moderator/Admin policies.
builder.Services.AddSupabaseAuth(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // UI at /scalar/v1
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Friendly root so the base URL isn't a bare 404 — this is an API, not a site.
app.MapGet("/", () => Results.Ok(new
{
    service = "MyQuiza API",
    status = "ok",
    health = "/health",
    api = "/api/v1",
    docs = "/scalar/v1 (development only)",
})).AllowAnonymous();

app.Run();
