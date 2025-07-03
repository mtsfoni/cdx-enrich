using Microsoft.Extensions.Logging;

namespace CdxEnrich.Logging
{
    public class ConsoleLogger(string categoryName, LogLevel minLevel = LogLevel.Information)
        : ILogger
    {
        // Private static object for thread-safe locking
        private static readonly object ConsoleLock = new object();
        
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= minLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var logMessage = formatter(state, exception);
            var output = logLevel >= LogLevel.Error ? Console.Error : Console.Out;

            // Build message components
            var timestamp = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]";
            var logLevelText = $"[{logLevel}]";
            var categoryText = $"[{categoryName}]";
            
            // Create complete log line
            var fullLine = $"{timestamp} {logLevelText} {categoryText} {logMessage}";
            
            lock (ConsoleLock)
            {
                // Calculate positions for colored section
                int logLevelStart = timestamp.Length + 1;
                int logLevelEnd = logLevelStart + logLevelText.Length;
                
                // Output part before LogLevel
                output.Write(fullLine.Substring(0, logLevelStart));
                
                // Store original color
                var originalColor = Console.ForegroundColor;
                
                // Set color for LogLevel
                Console.ForegroundColor = logLevel switch
                {
                    LogLevel.Error => ConsoleColor.Red,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Information => ConsoleColor.Cyan,
                    LogLevel.Debug => ConsoleColor.Gray,
                    LogLevel.Trace => ConsoleColor.DarkGray,
                    _ => Console.ForegroundColor
                };
                
                // Output LogLevel with color
                output.Write(logLevelText);
                
                // Restore original color
                Console.ForegroundColor = originalColor;
                
                // Output remainder of the line
                output.WriteLine(fullLine.Substring(logLevelEnd));
                
                if (exception != null)
                {
                    output.WriteLine($"Exception: {exception}");
                }
            }
        }
    }
    
    public class ConsoleLogger<T>(LogLevel minLevel = LogLevel.Information) : ConsoleLogger(typeof(T).Name, minLevel), ILogger<T>;
}