using Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace ServiceDefaults;

public static class ServiceDefaults
{
    public static void AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddMySqlDbContext<RefNotesContext>(connectionName: "main");
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
    }

    public static void AddOpenTelemetry(this IHostApplicationBuilder builder)
    {
        // Add Aspire tracing configuration
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.request.method", request.Method);
                            activity.SetTag("http.request.path", request.Path);
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response.status_code", response.StatusCode);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            activity.SetTag("http.client.request.method", request.Method.Method);
                        };
                        options.EnrichWithHttpResponseMessage = (activity, response) =>
                        {
                            activity.SetTag("http.client.response.status_code", (int)response.StatusCode);
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                        options.EnrichWithIDbCommand = (activity, command) =>
                        {
                            activity.SetTag("db.operation", command.CommandType.ToString());
                        };
                    })
                    .AddQuartzInstrumentation()
                    .AddRedisInstrumentation(options => { options.SetVerboseDatabaseStatements = true; })
                    .AddSource("Api.*") // Add custom activity sources for your application
                    .SetSampler(new AlwaysOnSampler()); // Configure sampling as needed
            });

        // Add logging with OpenTelemetry
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
    }
}
