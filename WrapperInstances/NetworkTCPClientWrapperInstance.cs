using System.IO;
using System.Net;
using System.Net.Sockets;

using PokeD.Core.Wrappers;

namespace PokeD.Server.Windows.WrapperInstances
{
    public class NetworkTCPClientWrapperInstance : INetworkTCPClient
    {
        public string IP => !IsDisposed && Client != null ? ((IPEndPoint) Client.Client.RemoteEndPoint).Address.ToString() : "";
        public bool Connected => !IsDisposed && Client != null && Client.Connected;
        public int DataAvailable => !IsDisposed && Client != null ? Client.Available : 0;


        private TcpClient Client { get; set; }
        private Stream Stream { get; set; }

        private bool IsDisposed { get; set; }


        public NetworkTCPClientWrapperInstance() { }

        public NetworkTCPClientWrapperInstance(TcpClient tcpClient)
        {
            Client = tcpClient;
            Client.SendTimeout = 5;
            Client.ReceiveTimeout = 5;
            Client.NoDelay = false;
            Stream = Client.GetStream();

        }


        public INetworkTCPClient Connect(string ip, ushort port)
        {
            if (Connected)
                Disconnect();

            Client = new TcpClient(ip, port) { SendTimeout = 5, ReceiveTimeout = 5, NoDelay = false };
            Stream = Client.GetStream();

            return this;
        }
        public INetworkTCPClient Disconnect()
        {
            if (Connected)
                Client.Client.Disconnect(false);

            return this;
        }

        public void Send(byte[] bytes, int offset, int count)
        {
            if (IsDisposed)
                return;

            try { Stream.Write(bytes, offset, count); }
            catch (IOException) { Dispose(); }
        }
        public int Receive(byte[] buffer, int offset, int count)
        {
            if (IsDisposed)
                return -1;

            try { return Stream.Read(buffer, offset, count); }
            catch (IOException) { Dispose(); return -1; }
        }

        public Stream GetStream()
        {
            return Stream;
        }

        public INetworkTCPClient NewInstance()
        {
            return new NetworkTCPClientWrapperInstance();
        }


        public void Dispose()
        {
            if(Connected)
                Disconnect();

            IsDisposed = true;

            Client?.Close();
            Stream?.Dispose();
        }
    }
}
