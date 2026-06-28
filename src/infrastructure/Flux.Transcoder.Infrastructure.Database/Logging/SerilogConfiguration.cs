using Serilog;
using Serilog.Events;

namespace Flux.Transcoder.Infrastructure.Database.Logging;

public static class SerilogConfiguration
{
    public static LoggerConfiguration CreateBaseConfiguration() =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(); // подгони URL/убери если Seq нет
}