namespace CdxEnrich
{
    /// <summary>
    /// Simple static logging for CLI output.
    /// Info messages only show when verbose mode is enabled.
    /// Warnings and errors always show (on stderr).
    /// </summary>
    public static class Log
    {
        private static bool _verbose = false;

        public static void SetVerbose(bool verbose)
        {
            _verbose = verbose;
        }

        /// <summary>
        /// Informational message - only shown when --verbose flag is set
        /// </summary>
        public static void Info(string message)
        {
            if (_verbose)
            {
                Console.WriteLine($"info: {message}");
            }
        }

        /// <summary>
        /// Warning message - always shown on stderr
        /// </summary>
        public static void Warn(string message)
        {
            Console.Error.WriteLine($"warn: {message}");
        }

        /// <summary>
        /// Error message - always shown on stderr
        /// </summary>
        public static void Error(string message)
        {
            Console.Error.WriteLine($"error: {message}");
        }
    }
}
