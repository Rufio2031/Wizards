using System.Text.Json;
using System.Text.Json.Serialization;
using Wizards.Api.Serialization;
using Wizards.Application.Extensions;
using Wizards.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        JsonSerializerOptions serializerOptions = options.JsonSerializerOptions;

        serializerOptions.PropertyNameCaseInsensitive = true;
        serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        serializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        serializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
        serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);

var app = builder.Build();

// Migrations run in-process on every start, in every environment, so a clone with no database file
// serves a working API with no tooling installed. The usual objection, several replicas racing to
// migrate one shared database, cannot happen here: the SQLite file is container-local and this
// single instance owns it.
await app.Services.InitializeDatabaseAsync(app.Lifetime.ApplicationStopping);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
