// Parte 1
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder.AddPrometheusExporter();

        builder.AddMeter("Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel");
        builder.AddView("http.server.request.duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05,
                    0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
            });
    });
var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

app.MapGet("/", () => "Hello OpenTelemetry! ticks:"
                      + DateTime.Now.Ticks.ToString()[^3..]);

app.Run();

// Parte 2

// using Microsoft.AspNetCore.Http.Features;
//
// var builder = WebApplication.CreateBuilder();
// var app = builder.Build();
//
// app.Use(async (context, next) =>
// {
//     var tagsFeature = context.Features.Get<IHttpMetricsTagsFeature>();
//     if (tagsFeature != null)
//     {
//         var source = context.Request.Query["utm_medium"].ToString() switch
//         {
//             "" => "none",
//             "social" => "social",
//             "email" => "email",
//             "organic" => "organic",
//             _ => "other"
//         };
//         tagsFeature.Tags.Add(new KeyValuePair<string, object?>("mkt_medium", source));
//     }
//
//     await next.Invoke();
// });
//
// app.MapGet("/", () => "Hello World!");
//
// app.Run();

// Parte 3
// using WebMetric;
// using WebMetric.Models;
//
// var builder = WebApplication.CreateBuilder(args);
//
// builder.Services.AddSingleton<ContosoMetrics>();
//
// var app = builder.Build();
//
// app.MapPost("/complete-sale", (SaleModel model, ContosoMetrics metrics) =>
// {
//     metrics.ProductSold(model.ProductName, model.QuantitySold);
// });
//
// app.Run();