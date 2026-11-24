using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;
using RaceDay.Api.Setup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddRaceDayJwt(builder.Configuration);
builder.Services.AddRaceDaySwagger();

builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Azure Blob Storage when a connection string is configured, otherwise local disk (wwwroot/uploads).
if (!string.IsNullOrWhiteSpace(builder.Configuration["AzureBlob:ConnectionString"]))
    builder.Services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
else
    builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRaceDaySwagger();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapHealthChecks("/health");
app.MapGet("/health/db", async (RaceDayDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { database = "connected" })
        : Results.Problem("Cannot connect to the database.", statusCode: 503));
app.MapControllers();

await DbInitializer.InitializeAsync(app.Services, app.Logger);

app.Run();
