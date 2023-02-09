using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Modeel
{
    public class TcpClientSSL
    {
        private TcpClient _client;
        private SslStream _stream;

        public TcpClientSSL(string server, int port)
        {
            _client = new TcpClient(server, port);
            _stream = new SslStream(_client.GetStream(), false, (sender, certificate, chain, sslPolicyErrors) => true);
            _stream.AuthenticateAsClient(server);
        }

        public void SendMessage(object message)
        {
            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json);
            _stream.Write(data, 0, data.Length);
        }

        public T? ReceiveMessage<T>()
        {
            var buffer = new byte[2048];
            var bytesRead = _stream.Read(buffer, 0, buffer.Length);
            var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}
