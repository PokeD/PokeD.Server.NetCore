using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using PokeD.Core.Wrappers;

namespace PokeD.Server.Windows.WrapperInstances
{
    public class NetworkTCPServerWrapperInstance : INetworkTCPServer
    {
        public ushort Port { get; }
        public bool AvailableClients => Listener.Pending();


        private TcpListener Listener { get; }


        internal NetworkTCPServerWrapperInstance() { }

        public NetworkTCPServerWrapperInstance(ushort port)
        {
            Port = port;
            Listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
        }


        public void Start()
        {
            Listener.Start();
        }

        public void Stop()
        {
            Listener.Stop();
        }

        public INetworkTCPClient AcceptNetworkTCPClient()
        {
            return new NetworkTCPClientWrapperInstance(Listener.AcceptTcpClient());
        }

        public Task<INetworkTCPClient> AcceptTCPClientAsync()
        {
            return new Task<INetworkTCPClient>(() => new NetworkTCPClientWrapperInstance(Listener.AcceptTcpClientAsync().Result));
        }

        public INetworkTCPServer NewInstance(ushort port)
        {
            return new NetworkTCPServerWrapperInstance(port);
        }


        public void Dispose()
        {
            Listener?.Stop();
        }
    }
}
