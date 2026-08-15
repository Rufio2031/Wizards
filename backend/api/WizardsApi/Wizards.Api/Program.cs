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

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync(app.Lifetime.ApplicationStopping);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();
