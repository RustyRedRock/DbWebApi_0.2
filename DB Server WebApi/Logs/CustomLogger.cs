namespace DB_Server_WebApi.Logs
{
    public enum LogSeverity
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public interface ILog
    {
        void WriteLine(LogSeverity level, string message, Exception exception = null);
    }

    public class ConsoleLogger : ILog
    {
        private void WriteColored(string text, ConsoleColor color)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = originalColor;
        }

        private void WriteLevel(LogSeverity level)
        {
            ConsoleColor color = ConsoleColor.Gray; // Default color
            switch (level)
            {
                case LogSeverity.Trace:
                    color = ConsoleColor.Gray;
                    break;
                case LogSeverity.Debug:
                    color = ConsoleColor.Blue;
                    break;
                case LogSeverity.Info: // Added Info level
                    color = ConsoleColor.Green;
                    break;
                case LogSeverity.Warning:
                    color = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Error:
                    color = ConsoleColor.Red;
                    break;
                case LogSeverity.Critical:
                    color = ConsoleColor.DarkRed;
                    break;
            }
            WriteColored($"[{level}]", color);
        }


        public void WriteLine(LogSeverity level, string message, Exception exception = null)
        {
            WriteLevel(level);
            Console.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] "));
            Console.Write(message);

            if (exception != null)
            {
                WriteColored("((( ", ConsoleColor.Red);
                Console.Write(exception.ToString());  // Log full exception details
                WriteColored(" )))", ConsoleColor.Red);
            }
            Console.WriteLine(); // Ensure a new line after each log entry
        }
    }

    // Example of adding file logging (Optional, but recommended for production)
    public class FileLogger : ILog
    {
        private readonly string _logFilePath;
        private readonly object _lock = new object(); // Lock for thread safety


        public FileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        //Helper method to ensure the directory
        private void EnsureDirectoryExists(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public void WriteLine(LogSeverity level, string message, Exception exception = null)
        {
            EnsureDirectoryExists(_logFilePath); //Ensure directory exists
            lock (_lock) //critical section for multithreading.
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(_logFilePath, true))
                    {
                        writer.Write($"[{level}]");
                        writer.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] "));
                        writer.Write(message);
                        if (exception != null)
                        {
                            writer.Write("((( " + exception.ToString() + " )))");
                        }
                        writer.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    // Handle the exception (e.g., log it to the console or another fallback)
                    Console.WriteLine($"Failed to write to log file: {ex}");
                }
            }
        }
    }

    //Combined Logger (writes both in file and Console)
    public class CombinedLogger : ILog
    {
        private readonly ILog[] _loggers;

        public CombinedLogger(params ILog[] loggers)
        {
            _loggers = loggers;
        }

        public void WriteLine(LogSeverity level, string message, Exception exception = null)
        {
            foreach (var logger in _loggers)
            {
                logger.WriteLine(level, message, exception);
            }
        }
    }

    // Factory for creating logger instances (Good practice)
    public static class LogFactory
    {
        public static ILog CreateLogger(LoggerType loggerType, string filePath = "app.log")
        {
            switch (loggerType)
            {
                case LoggerType.Console:
                    return new ConsoleLogger();
                case LoggerType.File:
                    return new FileLogger(filePath);
                case LoggerType.Combined:
                    return new CombinedLogger(new ConsoleLogger(), new FileLogger(filePath));
                default:
                    throw new ArgumentOutOfRangeException(nameof(loggerType), loggerType, null);
            }
        }
    }

    public enum LoggerType //For the LoggerFactory
    {
        Console,
        File,
        Combined
    }

    #region Exceptions

    // Base class for custom exceptions (Good Practice)
    public class BaseCustomException : Exception
    {
        public BaseCustomException(string message) : base(message) { }
        public BaseCustomException(string message, Exception innerException) : base(message, innerException) { }

        // Add any additional properties or methods common to all your exceptions
        // Example: ErrorCode, HttpStatusCode
        public int ErrorCode { get; set; }

    }

    public class UserNotFoundException : BaseCustomException
    {
        public UserNotFoundException(string message) : base(message) { }
    }

    public class InvalidTokenException : BaseCustomException
    {
        public InvalidTokenException() : this("Invalid token.") { } //  default message

        public InvalidTokenException(string message) : base(message) { }

        public InvalidTokenException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class InvalidCredentialsException : BaseCustomException
    {
        public InvalidCredentialsException() : base("Invalid username or password.") { } //Provide a default constrcutor
        public InvalidCredentialsException(string message) : base(message) { }
    }

    public class EmailNotConfirmedException : BaseCustomException
    {
        public EmailNotConfirmedException() : base("Email not confirmed.") { } //Provide a default constructor
        public EmailNotConfirmedException(string message) : base(message) { }
    }

    public class DuplicateUserException : BaseCustomException
    {
        public DuplicateUserException() : base("User already exists.") { } //Provide a default constructor.

        public DuplicateUserException(string message) : base(message) { }

        public DuplicateUserException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class EmailAlreadyConfirmedException : Exception
    {
        public EmailAlreadyConfirmedException() : base() { }
        public EmailAlreadyConfirmedException(string message) : base(message) { }
        public EmailAlreadyConfirmedException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class LockedOutException : Exception
    {
        public LockedOutException() : base() { }
        public LockedOutException(string message) : base(message) { }
        public LockedOutException(string message, Exception inner) : base(message, inner) { }
    }
    public class TwoFactorRequiredException : Exception
    {
        public TwoFactorRequiredException() : base() { }
        public TwoFactorRequiredException(string message) : base(message) { }
        public TwoFactorRequiredException(string message, Exception inner) : base(message, inner) { }
    }
    #endregion

    // Example Usage (in your main application)
    /*
    public class ExampleUsage
    {
        public static void Main(string[] args)
        {
            //Use LoggerFactory to create loggers
            ILogger consoleLogger = LoggerFactory.CreateLogger(LoggerType.Console);
            ILogger fileLogger = LoggerFactory.CreateLogger(LoggerType.File, "logs/application.log");  // Specify log file path
            ILogger combinedLogger = LoggerFactory.CreateLogger(LoggerType.Combined, "logs/combined.log");


            consoleLogger.WriteLine(LogLevel.Info, "Starting the application.");
            fileLogger.WriteLine(LogLevel.Warning, "This is a warning message.");

            try
            {
                // Simulate an error
                throw new InvalidCredentialsException();
            }
            catch (InvalidCredentialsException ex)
            {
                combinedLogger.WriteLine(LogLevel.Error, "An error occurred.", ex);
            }


            try
            {
                // Simulate another error
                throw new Exception("A generic error occurred.");
            }
            catch (Exception ex)
            {
                fileLogger.WriteLine(LogLevel.Critical, "An unexpected error occurred.", ex);
            }


            consoleLogger.WriteLine(LogLevel.Debug, "Application finished.");
        }
    }
    */
}
