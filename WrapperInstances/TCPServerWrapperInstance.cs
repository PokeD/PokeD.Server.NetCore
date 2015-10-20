using System.Net;
using System.Net.Sockets;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class TCPListenerClass : ITCPListener
    {
        public ushort Port { get; }
        public bool AvailableClients => Listener.Poll(0, SelectMode.SelectRead);

        private bool IsDisposed { get; set; }

        private Socket Listener { get; }


        internal TCPListenerClass(ushort port)
        {
            Port = port;

            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Listener.NoDelay = true;

            Listener.Bind(new IPEndPoint(IPAddress.Any, Port));
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

            return new TCPClientClass(Listener.Accept());
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            Listener?.Dispose();
        }
    }

    public class ITCPServerWrapperrInstance : ITCPListenerWrapper
    {  
        public ITCPListener CreateTCPListener(ushort port) { return new TCPListenerClass(port); }
    }
}
