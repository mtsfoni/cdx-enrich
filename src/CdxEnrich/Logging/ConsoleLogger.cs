using Microsoft.Extensions.Logging;

namespace CdxEnrich.Logging
{
    public class ConsoleLogger<T> : ILogger<T>
    {
        private readonly LogLevel _minLevel;
        private readonly string _categoryName;

        public ConsoleLogger(LogLevel minLevel = LogLevel.Information)
        {
            _minLevel = minLevel;
            _categoryName = typeof(T).Name;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var logMessage = formatter(state, exception);
            var output = logLevel >= LogLevel.Error ? Console.Error : Console.Out;

            output.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{_categoryName}] {logMessage}");

            if (exception != null)
            {
                output.WriteLine($"Exception: {exception}");
            }
        }
    }
}
