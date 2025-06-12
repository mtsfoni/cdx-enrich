using Microsoft.Extensions.Logging;

namespace CdxEnrich.Logging
{
    public class ConsoleLogger(string categoryName, LogLevel minLevel = LogLevel.Information)
        : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= minLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var logMessage = formatter(state, exception);
            var output = logLevel >= LogLevel.Error ? Console.Error : Console.Out;

            output.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{categoryName}] {logMessage}");

            if (exception != null)
            {
                output.WriteLine($"Exception: {exception}");
            }
        }
    }
    
    public class ConsoleLogger<T>(LogLevel minLevel = LogLevel.Information) : ConsoleLogger(typeof(T).Name, minLevel), ILogger<T>;
}
