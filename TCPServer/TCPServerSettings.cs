using System.Net;

namespace TCPServer
{
    public class TCPServerSettings
    {
        public static int TCPServerListningPort { get; set; } = 8100;
        public static IPAddress TCPServerIP { get; set; } = IPAddress.Any; // just for information , it will be upated with ip.Any
    }
}
