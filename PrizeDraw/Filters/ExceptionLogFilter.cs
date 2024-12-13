using Microsoft.AspNetCore.Mvc.Filters;
using Serilog.Events;
using Serilog;

namespace PrizeDraw.Filters;

public class ExceptionLogFilter : IAsyncExceptionFilter
{
    //private readonly ILogger _logger;

    //public ExceptionLogFilter(ILogger logger)
    //{
    //    _logger = logger;
    //}

    public Task OnExceptionAsync(ExceptionContext context)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Error)
            .MinimumLevel.Override("Quartz", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File(
                Path.Combine("logs", "all", $".txt"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Debug,
                retainedFileCountLimit: 7))
            .WriteTo.Async(c => c.File(
                Path.Combine("logs", "error", $".txt"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error,
                retainedFileCountLimit: 7))
            .WriteTo.Async(c => c.Console())
            .CreateLogger();

        Log.Error(context.Exception.ToString());
        return Task.CompletedTask;
    }
}
