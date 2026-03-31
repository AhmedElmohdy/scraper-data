using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using ElasticTest.Config;
using ElasticTest.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Swagger & MVC
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Bind config to ElasticOptions
builder.Services.Configure<ElasticOptions>(
    builder.Configuration.GetSection("Elastic"));

// Register two clients explicitly
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<ElasticOptions>>().Value;

    var settings = new ElasticsearchClientSettings(new Uri(cfg.Products.Uri))
        .Authentication(new BasicAuthentication(cfg.Products.Username, cfg.Products.Password))
        .DefaultIndex(cfg.Products.Index)
        .DisableDirectStreaming()
        .ServerCertificateValidationCallback((_, __, ___, ____) => true);

    return new ElasticsearchClient(settings);
});

// Register alerts client separately
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<ElasticOptions>>().Value;

    var settings = new ElasticsearchClientSettings(new Uri(cfg.HistoricalAlerts.Uri))
        .Authentication(new BasicAuthentication(cfg.HistoricalAlerts.Username, cfg.HistoricalAlerts.Password))
        .DefaultIndex(cfg.HistoricalAlerts.Index)
        .DisableDirectStreaming()
        .ServerCertificateValidationCallback((_, __, ___, ____) => true);

    return new ElasticsearchClient(settings);
});

// Register your wrapper service that consumes both
builder.Services.AddSingleton<ElasticService>();
builder.Services.AddSingleton<AlertElasticService>();
var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
