using System.IO;
using System.Net;
using System.Net.Sockets;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class SafeNetworkStream : Stream
    {
        private Socket Socket { get; }
        private NetworkStream Stream { get; }

        public override bool CanRead => Stream.CanRead;
        public override bool CanSeek => Stream.CanSeek;
        public override bool CanWrite => Stream.CanWrite;
        public override long Length => Stream.Length;
        public override long Position { get { return Stream.Position; } set { Stream.Position = value; } }


        public SafeNetworkStream(Socket socket)
        {
            Socket = socket;
            Stream = new NetworkStream(Socket);
        }



        public override long Seek(long offset, SeekOrigin origin) => Stream.Seek(offset, origin);
        public override void SetLength(long value) { Stream.SetLength(value); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                var bytesReceived = 0;
                while (bytesReceived < count)
                    bytesReceived += Socket.Receive(buffer, bytesReceived, count - bytesReceived, 0);

                return bytesReceived;
            }
            catch (IOException) { return -1; }
            catch (SocketException) { return -1; }
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                var bytesSend = 0;
                while (bytesSend < count)
                    bytesSend += Socket.Send(buffer, bytesSend, count - bytesSend, 0);
            }
            catch (IOException) { }
            catch (SocketException) { }
        }
        public override void Flush() { Stream.Flush(); }
    }

    public class SocketTCPClient: ITCPClient
    {
        public string IP => !IsDisposed && Client != null && Client.Connected ? (Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() : "";
        public ushort Port => (ushort)(!IsDisposed && Client != null && Client.Connected ? (Client.RemoteEndPoint as IPEndPoint)?.Port : 0);
        public bool Connected => !IsDisposed && Client != null && Client.Connected;
        public int DataAvailable => !IsDisposed && Client != null ? Client.Available : 0;

        private Socket Client { get; }
        private Stream Stream { get; set; }

        private bool IsDisposed { get; set; }


        public SocketTCPClient() { Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true }; }
        internal SocketTCPClient(Socket socket) { Client = socket; Stream = new SafeNetworkStream(Client); }

        public Stream GetStream() { return Stream; }

        public ITCPClient Connect(string ip, ushort port)
        {
            if (Connected)
                Disconnect();

            Client.Connect(ip, port);
            Stream = new SafeNetworkStream(Client);

            return this;
        }
        public ITCPClient Disconnect()
        {
            if (Connected)
                Client.Disconnect(false);

            return this;
        }

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
