using System.IO;
using System.Net;
using System.Net.Sockets;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class SocketTCPClient: ITCPClient
    {
        public int RefreshConnectionInfoTime { get; set; }

        public string IP => !IsDisposed && Client != null && Client.Connected ? (Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() : "";
        public ushort Port => (ushort)(!IsDisposed && Client != null && Client.Connected ? (Client.RemoteEndPoint as IPEndPoint)?.Port : 0);
        public bool Connected => !IsDisposed && Client != null && Client.Connected;
        public int DataAvailable => !IsDisposed && Client != null ? Client.Available : 0;

        private Socket Client { get; }
        private Stream Stream { get; set; }

        private bool IsDisposed { get; set; }


        public SocketTCPClient()
        {
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        }
        internal SocketTCPClient(Socket socket)
        {
            Client = socket;
            Stream = new NetworkStream(Client);
        }
        
        public ITCPClient Connect(string ip, ushort port)
        {
            if (Connected)
                Disconnect();

            Client.Connect(ip, port);
            Stream = new NetworkStream(Client);

            return this;
        }
        public ITCPClient Disconnect()
        {
            if (Connected)
                Client.Disconnect(false);

            return this;
        }

        public int Write(byte[] buffer, int offset, int count)
        {
            if (IsDisposed)
                return -1;

            try
            {
                return Client.Send(buffer, offset, count, SocketFlags.None);
                //var bytesSend = 0;
                //while (bytesSend < count)
                //    bytesSend += Client.Send(buffer, bytesSend, count - bytesSend, 0);
                //
                //return bytesSend;
            }
            catch (IOException) { Dispose(); return -1; }
            catch (SocketException) { Dispose(); return -1; }
        }
        public int Read(byte[] buffer, int offset, int count)
        {
            if (IsDisposed)
                return -1;

            try
            {
                return Client.Receive(buffer, offset, count, SocketFlags.None);
                //var bytesReceived = 0;
                //while (bytesReceived < count)
                //    bytesReceived += Client.Receive(buffer, bytesReceived, count - bytesReceived, 0);
                //
                //return bytesReceived;
            }
            catch (IOException) { Dispose(); return -1; }
            catch (SocketException) { Dispose(); return -1; }
        }

        public Stream GetStream() { return Stream; }

        public void Dispose()
        {
            if (Connected)
                Disconnect();

            IsDisposed = true;

            Client?.Dispose();
            Stream?.Dispose();
        }
    }

    public class TCPClientFactoryInstance : ITCPClientFactory
    {
        public ITCPClient Create() => new SocketTCPClient();
        internal static ITCPClient CreateTCPClient(Socket socket) => new SocketTCPClient(socket);
    }
}
