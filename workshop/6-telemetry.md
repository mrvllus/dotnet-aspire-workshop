# Telemetry Module

## Introduction

In this module, we'll implement comprehensive observability features in our weather application using .NET Aspire's built-in OpenTelemetry support. We'll cover three main aspects:

1. **Structured Logging**: Implementing context-rich logging that makes it easier to trace and debug requests through the system
2. **Custom Metrics**: Creating and tracking application-specific metrics like request counts, durations, and cache performance
3. **Distributed Tracing**: Adding request tracing to follow operations across service boundaries

You'll learn how to:

- Create a diagnostic infrastructure for metrics and tracing
- Implement structured logging with scopes and semantic context
- Add custom metrics for tracking application behavior
- Use distributed tracing to follow requests through the system
- Test and observe the telemetry data in the .NET Aspire dashboard
- Integrate with external observability platforms

## OpenTelemetry in .NET Aspire ServiceDefaults

.NET Aspire's ServiceDefaults project automatically configures OpenTelemetry for your application. When you call `builder.AddServiceDefaults()`, it:

1. Configures structured logging with OpenTelemetry
2. Sets up distributed tracing with common instrumentation:
   - ASP.NET Core
   - HTTP client calls
   - Runtime metrics
   - Service discovery
3. Configures metrics collection and export
4. Enables integration with the .NET Aspire dashboard

This means you don't need to manually configure the basic OpenTelemetry infrastructure. You can focus on adding your application-specific telemetry.

## Implementing Custom Metrics

We'll create a diagnostic infrastructure to track specific metrics about our weather service:

1. Create a new file `NwsManagerDiagnostics.cs` in the `Api/Data` folder.
2. Add the following code to define custom metrics:

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Api.Diagnostics
{
    public class NwsManagerDiagnostics
    {
        private static readonly Meter meter = new Meter("NwsManagerMetrics", "1.0");
        public static readonly Counter<int> forecastRequestCounter = meter.CreateCounter<int>("forecast_requests_total", "Total number of forecast requests");
        public static readonly Histogram<double> forecastRequestDuration = meter.CreateHistogram<double>("forecast_request_duration_seconds", "Histogram of forecast request durations");
        public static readonly Counter<int> failedRequestCounter = meter.CreateCounter<int>("failed_requests_total", "Total number of failed requests");
        public static readonly Counter<int> cacheHitCounter = meter.CreateCounter<int>("cache_hits_total", "Total number of cache hits");
        public static readonly Counter<int> cacheMissCounter = meter.CreateCounter<int>("cache_misses_total", "Total number of cache misses");
        public static readonly ActivitySource activitySource = new ActivitySource("NwsManager");
    }
}
```

### Understanding the Metrics

Our custom metrics track several key aspects of the weather service:

- **Request Counts**: Total number of forecast requests made
- **Request Duration**: How long forecast requests take to complete
- **Error Rates**: Number of failed requests
- **Cache Performance**: Cache hit and miss rates
- **Distributed Tracing**: Activity source for following requests

3. Register the custom meter and activity source in Program.cs:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m.AddMeter("NwsManagerMetrics"))
    .WithTracing(m => m.AddSource("NwsManager"));
```

## Implementing Telemetry in NwsManager

Now we'll update the NwsManager class to use our metrics and implement structured logging. The implementation combines several observability features:

```csharp
public async Task<Forecast[]> GetForecastByZoneAsync(string zoneId)
{
    // Create a logging scope with structured data
    using var logScope = logger.BeginScope(new Dictionary<string, object>
    {
        ["ZoneId"] = zoneId,
        ["RequestNumber"] = Interlocked.Increment(ref forecastCount)
    });

    // Record the request in our metrics
    NwsManagerDiagnostics.forecastRequestCounter.Add(1);
    var stopwatch = Stopwatch.StartNew();

    // Create a trace activity
    using var activity = NwsManagerDiagnostics.activitySource.StartActivity("GetForecastByZoneAsync");
    activity?.SetTag("zone.id", zoneId);

    logger.LogInformation("🚀 Starting forecast request for zone {ZoneId}", zoneId);

    try 
    {
        // ... API call logic ...
        stopwatch.Stop();
        
        // Record the request duration
        NwsManagerDiagnostics.forecastRequestDuration.Record(stopwatch.Elapsed.TotalSeconds);
        activity?.SetTag("request.success", true);

        logger.LogInformation(
            "📊 Retrieved forecast for zone {ZoneId} in {Duration:N0}ms with {PeriodCount} periods",
            zoneId,
            duration.TotalMilliseconds,
            forecasts?.Properties?.Periods?.Count ?? 0
        );

        return forecasts;
    }
    catch (HttpRequestException ex)
    {
        // Record failures in our metrics
        NwsManagerDiagnostics.failedRequestCounter.Add(1);
        activity?.SetTag("request.success", false);
        
        logger.LogError(
            ex,
            "❌ Failed to retrieve forecast for zone {ZoneId}. Status: {StatusCode}",
            zoneId,
            ex.StatusCode
        );
        throw;
    }
}
```

This implementation shows how our custom metrics work with structured logging:

- Request metrics track performance and error rates
- Logging scopes group related log entries
- Trace activities connect logs across service boundaries
- Log messages include structured data for better analysis

## Testing the Implementation

1. Run the application using the .NET Aspire dashboard
2. Open the "Structured" tab in the dashboard
3. Click on several different cities in the weather app
4. Observe the telemetry data:

### Structured Logs View

- In the "Structured" tab:
  - Set different log level filters
  - Search by zone ID or other properties
  - View full structured data in "Details"
  - See how log entries are grouped by request scope

### Metrics View

- In the "Metrics" tab, observe:
  - `forecast_requests_total`
  - `forecast_request_duration_seconds`
  - `failed_requests_total`
  - `cache_hits_total`
  - `cache_misses_total`

### Traces View

- In the "Traces" tab:
  - Find a trace for GetForecastByZoneAsync
  - See how logs connect to traces
  - Observe the complete request flow
  - View timing and success/failure data

## Telemetry Integrations

.NET Aspire's OpenTelemetry infrastructure makes it easy to integrate with various observability platforms. Here are some popular options:

### Cloud Provider Solutions

- **Azure Monitor Application Insights**: Native integration with Azure services. [Learn more](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-overview)
- **AWS CloudWatch**: Monitoring for AWS deployments. [Learn more](https://aws.amazon.com/cloudwatch/)
- **Google Cloud Operations (formerly Stackdriver)**: Monitoring for GCP. [Learn more](https://cloud.google.com/stackdriver)

### Third-Party Services

- **New Relic**: Full-stack observability platform. [Learn more](https://newrelic.com/solutions/opentelemetry)
- **Datadog**: Infrastructure and application monitoring. [Learn more](https://docs.datadoghq.com/opentelemetry/)
- **Honeycomb**: Observability for distributed systems. [Learn more](https://www.honeycomb.io/opentelemetry)
- **Grafana**: Visualization and analytics platform. [Learn more](https://grafana.com/docs/grafana-cloud/monitor-infrastructure/integrations/integration-reference/integration-opentelemetry/)
- **Dynatrace**: Application performance monitoring. [Learn more](https://www.dynatrace.com/support/help/extend-dynatrace/opentelemetry)

### Example: Adding Azure Monitor

To send telemetry to Azure Application Insights:

1. Install the NuGet package:

   ```bash
   dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore
   ```

2. Update your Program.cs:

   ```csharp
   // In ServiceDefaults/Extensions.cs
   if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
   {
       builder.Services.AddOpenTelemetry()
           .UseAzureMonitor();
   }
   ```

3. Set your connection string in configuration or environment variables:

   ```json
   {
     "APPLICATIONINSIGHTS_CONNECTION_STRING": "your-connection-string"
   }
   ```

For other integrations, consult their respective documentation for .NET OpenTelemetry setup instructions.

**Next**: [Module #7: Database Integration](7-database.md)
