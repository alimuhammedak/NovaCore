using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace NovaCore.Serilog;

/// <summary>Makes Serilog the host's logging provider.</summary>
public static class SerilogHostingExtensions
{
    /// <summary>
    /// Replaces the default logging with Serilog: console output, plus Seq when <c>Seq:ServerUrl</c> is configured.
    /// Pair it with Serilog's <c>app.UseSerilogRequestLogging()</c> for one summary line per HTTP request.
    /// </summary>
    public static IHostApplicationBuilder AddCoreLogging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, configuration) =>
        {
            // formatProvider: null is Serilog's own default (the culture is resolved when an event is rendered); it is
            // spelled out only for CA1305 (docs/plan/DECISIONS.md D-020).
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}",
                    formatProvider: null);

            // Logs ship to Seq only when a server URL is configured — never a hard dependency.
            var seqUrl = builder.Configuration["Seq:ServerUrl"];
            if (!string.IsNullOrWhiteSpace(seqUrl))
                configuration.WriteTo.Seq(seqUrl, apiKey: builder.Configuration["Seq:ApiKey"], formatProvider: null);
        });

        return builder;
    }
}
