using System.Text.Encodings.Web;
using System.Text.Json;
using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using TestApp.Models;
using TestApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 64 * 1024 * 1024;
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
        options.JsonSerializerOptions.AllowTrailingCommas = true;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var message = string.Join("; ", context.ModelState
                .SelectMany(kvp => kvp.Value?.Errors ?? [])
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m)));

            return new OkObjectResult(ParseResponse.Fail(
                ErrorCodes.InvalidRequest,
                string.IsNullOrWhiteSpace(message) ? "Invalid request body." : message!));
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IValidator<ParseRequest>, ParseRequestValidator>();
builder.Services.AddScoped<IParseService, ParseService>();

var app = builder.Build();

await EnsureSchemaAsync(app.Configuration);

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "TestApp API v1");
    options.RoutePrefix = "api/swagger";
});

app.MapGet("/", () => Results.Redirect("/api/swagger"));
app.MapControllers();

app.Run();

static async Task EnsureSchemaAsync(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("Db")
        ?? throw new InvalidOperationException("Connection string 'Db' is not configured.");

    const string sql =
        """
        CREATE TABLE IF NOT EXISTS elements (
            id           BIGSERIAL PRIMARY KEY,
            attr_value   TEXT NOT NULL,
            element_html TEXT NOT NULL
        );
        """;

    Exception? last = null;
    for (var attempt = 1; attempt <= 30; attempt++)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync(sql);
            return;
        }
        catch (Exception ex)
        {
            last = ex;
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    throw new InvalidOperationException("Cannot initialize PostgreSQL schema.", last);
}
