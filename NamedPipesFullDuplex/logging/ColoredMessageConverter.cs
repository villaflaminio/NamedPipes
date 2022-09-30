using System;
using System.IO;
using log4net.Core;
using log4net.Layout.Pattern;

namespace NamedPipesFullDuplex.logging
{
    /// <summary>
    ///     Convert compute the logging messages, fully customizable, instead of xml configuration.
    /// </summary>
    public class ColoredMessageConverter : PatternLayoutConverter
    {
        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            Console.Write("{0} | {1} ", DateTime.Now.ToString(), loggingEvent.LoggerName);
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
            Console.Write("{0} " , loggingEvent.Level);


            // Reset Console Colors.
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(" | {0} \n", loggingEvent.RenderedMessage);
        }
    }
}