namespace Modeel.Log
{
    public static class LoggerInfo
    {
        public static readonly string exception = "ERROR";
        public static readonly string warning = "WARNING";
        public static readonly string methodEntry = "ENTERING METHOD";
        public static readonly string methodExit = "EXITING METHOD";
        public static readonly string msgSendLocal = "SENDING MESSAGE LOCALY: ";
        public static readonly string msgReceivLocal = "RECEIVING MESSAGE LOCALY: ";
        public static readonly string windowCreated = "WINDOW CREATED: ";
        public static readonly string windowClosed = "WINDOW CLOSED: ";
        public static readonly string tcpServer = "TCP-SERVER: ";
        public static readonly string sslServer = "SSL-SERVER: ";
        public static readonly string sslClient = "SSL-CLIENT: ";
        public static readonly string tcpClient = "TCP-CLIENT: ";
        public static readonly string P2PSSL = "P2PSSL: ";
        public static readonly string P2P = "P2P: ";
        public static readonly string socketMessage = "SOCKET MESSAGE";
        public static readonly string fileTransfering = "FILE TRANSFERING";
    }
}
