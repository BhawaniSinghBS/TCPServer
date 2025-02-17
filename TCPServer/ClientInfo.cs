using System.Net;
using System.Net.Sockets;

namespace TCPServer
{
    public class ClientInfo
    {
        public TcpClient Client { get; set; }
        public string ClientId { get; set; }
        public DateTime ConnectedAt { get; set; }
        public IPAddress IPAddress { get; set; }
        public int Port { get; set; }
    }
}
