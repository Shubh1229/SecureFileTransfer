using SecureFileTransfer.src.setup;

namespace SecureFileTransfer.src.logging
{
    public static class DebugLogger
    {
        private static readonly string PathToLog = AppPaths.DebugLogPath;
        private static readonly object _lock = new();

        public static void Log(string message)
        {
            AppPaths.EnsureAppDirectoryExists();

            string line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC] {message}";

            lock (_lock)
            {
                File.AppendAllText(PathToLog, line + Environment.NewLine);
            }
        }

        public static void LogError(string context, Exception ex)
        {
            AppPaths.EnsureAppDirectoryExists();

            string line =
                $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC] ERROR in {context}: {ex.Message}{Environment.NewLine}{ex}";

            lock (_lock)
            {
                File.AppendAllText(PathToLog, line + Environment.NewLine + Environment.NewLine);
            }
        }

        public static string GetPath()
        {
            return PathToLog;
        }

        public static void Separator(string title = "")
        {
            Log(string.IsNullOrWhiteSpace(title)
                ? "--------------------------------------------------"
                : $"-------------------- {title} --------------------");
        }
    }
}