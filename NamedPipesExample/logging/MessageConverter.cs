using System;
using System.IO;
using log4net.Core;
using log4net.Layout.Pattern;

namespace NamedPipesExample.logging
{
    /// <summary>
    ///     Compute the logging messages, fully customizable, instead of xml configuration.
    /// </summary>
    public class MessageConverter : PatternLayoutConverter
    {
        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            switch (loggingEvent.Level.Name)
            {
                case "DEBUG":
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case "WARN":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case "INFO":
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case "ERROR":
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Red;
                    break;
                case "FATAL":
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.BackgroundColor = ConsoleColor.White;
                    break;
            }

            writer.Write("{0}", loggingEvent.Level);

            // Reset Console Colors.
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;

            writer.Write(" | {0} \n", loggingEvent.RenderedMessage);
        }
    }
}