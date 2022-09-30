namespace NamedPipesExample.logging
{
    /// <summary>
    ///     Class used to map the logging levels.
    /// </summary>
    internal class LoggingLevel
    {
        private enum LoggingLevels
        {
            TRACE = 0,
            DEBUG,
            INFO,
            WARN,
            ERROR,
            FATAL
        }
    }
}