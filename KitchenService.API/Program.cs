using KitchenService.Infrastructure;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.Grafana.Loki;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var configuration = builder.Configuration;

var serviceName = "FastTechFoods.KitchenServiceAPI";

var lokiStringConnection = Environment.GetEnvironmentVariable("CONNECTION_LOKI") ??
                "http://localhost:3100";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithSpan()
    .Enrich.WithProperty("Application", serviceName)
    .WriteTo.GrafanaLoki(
        uri: lokiStringConnection,
        labels: new[]
        {
            new LokiLabel { Key = "app", Value = serviceName }
        })
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
var openTelemetryConnection = Environment.GetEnvironmentVariable("CONNECTION_OPENTELEMETRY") ??
                "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
            .AddSource("MassTransit")
            .AddOtlpExporter(options =>
            {
                // 👇 FORÇANDO A URL E O PROTOCOLO 👇
                options.Endpoint = new Uri(openTelemetryConnection);
                options.Protocol = OtlpExportProtocol.Grpc;
            });
    });
builder.Services.AddControllers();
builder.Services.AddInfraestructureModule(configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
