namespace Logger
{
    public class LogEntry
    {
        public string Message { get; set; }
        public LogLevel LogLevel { get; set; }
        public int LineNumber { get; set; }
        public string CallingFilePath { get; set; }
        public string CallingMethod { get; set; }
        public string ThreadName { get; set; }
        public string DateTime { get; set; }
    }
}