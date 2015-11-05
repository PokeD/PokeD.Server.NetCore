using System.Net;
using System.Net.Sockets;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class SocketTCPListener : ITCPListener
    {
        public ushort Port { get; }
        public bool AvailableClients => Listener.Poll(0, SelectMode.SelectRead);

        private bool IsDisposed { get; set; }

        private Socket Listener { get; }


        internal SocketTCPListener(ushort port)
        {
            Port = port;

            var endpoint = new IPEndPoint(IPAddress.Any, Port);
            Listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Listener.NoDelay = true;

            Listener.Bind(endpoint);
        }

        public void Start()
        {
            if (IsDisposed)
                return;

            Listener.Listen(1000);
        }

        public void Stop()
        {
            if (IsDisposed)
                return;

            Listener.Close();
        }

        public ITCPClient AcceptTCPClient()
        {
            if (IsDisposed)
                return null;

            return TCPClientWrapperInstance.CreateTCPClient(Listener.Accept());
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            Listener?.Dispose();
        }
    }

    public class TCPListener : ITCPListener
    {
        public ushort Port { get; }
        public bool AvailableClients => Listener.Pending();

        private bool IsDisposed { get; set; }

        private TcpListener Listener { get; }


        internal TCPListener(ushort port)
        {
            Port = port;

            var endpoint = new IPEndPoint(IPAddress.Any, Port);
            Listener = new TcpListener(endpoint);
        }

        public void Start()
        {
            if (IsDisposed)
                return;

            Listener.Start();
        }

        public void Stop()
        {
            if (IsDisposed)
                return;

            Listener.Stop();
        }

        public ITCPClient AcceptTCPClient()
        {
            if (IsDisposed)
                return null;

            return TCPClientWrapperInstance.CreateTCPClient(Listener.AcceptSocket());
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            Listener?.Server.Dispose();
        }
    }

    public class TCPServerWrapperInstance : ITCPListenerWrapper
    {  
        public ITCPListener CreateTCPListener(ushort port) { return new SocketTCPListener (port); }
    }
}
