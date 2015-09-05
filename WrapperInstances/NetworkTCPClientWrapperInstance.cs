using System.IO;
using System.Net;
using System.Net.Sockets;

using PokeD.Core.Wrappers;

namespace PokeD.Server.Windows.WrapperInstances
{
    public class NetworkTCPClientWrapperInstance : INetworkTCPClient
    {
        public string IP => !IsDisposed && Client != null ? ((IPEndPoint)Client.Client.RemoteEndPoint).Address.ToString() : "";
        public bool Connected => !IsDisposed && Client != null && Client.Connected;
        public int DataAvailable => !IsDisposed && Client != null ? Client.Available : 0;


        private TcpClient Client { get; set; }
        private NetworkStream WriteStream { get; set; }
        private StreamReader StreamReader { get; set; }

        private bool IsDisposed { get; set; }


        public NetworkTCPClientWrapperInstance() { }

        internal NetworkTCPClientWrapperInstance(TcpClient tcpClient)
        {
            Client = tcpClient;
            Client.SendTimeout = 2;
            Client.ReceiveTimeout = 2;
            WriteStream = new NetworkStream(Client.Client);
            StreamReader = new StreamReader(WriteStream);
        }

        public void Connect(string ip, ushort port)
        {
            Client = new TcpClient(ip, port) { SendTimeout = 2, ReceiveTimeout = 2 };
            WriteStream = new NetworkStream(Client.Client);
            StreamReader = new StreamReader(WriteStream);
        }
        public void Disconnect()
        {
            if (Connected)
                Client.Client.Disconnect(false);
        }

        public void Send(byte[] bytes, int offset, int count)
        {
            if (!IsDisposed)
            {
                try { WriteStream.Write(bytes, offset, count); }
                catch (IOException) { Disconnect(); }
            }
        }
        public int Receive(byte[] buffer, int offset, int count)
        {
            if (!IsDisposed)
            {
                try { return WriteStream.Read(buffer, offset, count); }
                catch (IOException) { Disconnect(); return -1; }
            }
            else
                return -1;
        }


        public string ReadLine()
        {
            try { return StreamReader.ReadLine(); }
            catch (IOException) { Disconnect(); return ""; }
        }



        public INetworkTCPClient NewInstance()
        {
            return new NetworkTCPClientWrapperInstance();
        }


        public void Dispose()
        {
            IsDisposed = true;

            Client?.Close();
            WriteStream?.Dispose();
        }
    }
}
