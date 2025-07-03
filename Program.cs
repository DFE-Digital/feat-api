using System.Text.Json.Serialization;
using feat.api.Configuration;
using feat.api.Repositories;
using feat.api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


// Load configuration
builder.Services.Configure<AzureOptions>(
    builder.Configuration.GetSection(AzureOptions.Azure));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.WriteIndented = true;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddOpenApi();
builder.Services.AddSingleton<ISearchService, SearchService>();

// Setup our HTTP client
builder.Services.AddHttpClient("httpClient");
builder.Services.AddSingleton<HttpClientRepository>();

var app = builder.Build();
app.UseHttpsRedirection();
app.MapControllers();
app.MapOpenApi();
app.MapScalarApiReference();

app.Run();