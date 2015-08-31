using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using PokeD.Core.Wrappers;

namespace PokeD.Server.Windows.WrapperInstances
{
    public class NetworkTCPServerWrapperInstance : INetworkTCPServer
    {
        public ushort Port { get; set; }
        public bool AvailableClients {  get { return Listener.Pending(); } }


        private TcpListener Listener { get; set; }
        private bool IsDisposed { get; set; }


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

        public INetworkTcpClient AcceptNetworkTCPClient()
        {
            return new NetworkTCPClientWrapperInstance(Listener.AcceptTcpClient());
        }

        public Task<INetworkTcpClient> AcceptTcpClientAsync(byte[] bytes, int offset, int count)
        {
            return new Task<INetworkTcpClient>(() => new NetworkTCPClientWrapperInstance(Listener.AcceptTcpClientAsync().Result));
        }

        public INetworkTCPServer NewInstance(ushort port)
        {
            return new NetworkTCPServerWrapperInstance(port);
        }


        public void Dispose()
        {
            IsDisposed = true;

            Thread.Sleep(500);

            if (Listener != null)
                Listener.Stop();
        }
    }
}
