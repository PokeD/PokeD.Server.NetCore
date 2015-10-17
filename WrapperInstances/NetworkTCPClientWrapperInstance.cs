using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class NetworkTCPClientWrapperInstance : INetworkTCPClient
    {
        #region Connection Stuff
        private static int RefreshConnectionInfoTimeStatic { get; set; } = 5000;
        private static Stopwatch ConnectedTCPRefresh { get; } = Stopwatch.StartNew();

        private static TcpConnectionInformation[] _connectedTCPs;
        private static TcpConnectionInformation[] ConnectedTCPs
        {
            get
            {
                if (ConnectedTCPRefresh.ElapsedMilliseconds > RefreshConnectionInfoTimeStatic)
                    UpdateConnectedTCPs();
                
                return _connectedTCPs;
            }
            set { _connectedTCPs = value; }
        }
        private static void UpdateConnectedTCPs()
        {
            ConnectedTCPRefresh.Restart();
            ConnectedTCPs = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
        }
        #endregion Connection Stuff

        public int RefreshConnectionInfoTime { get { return RefreshConnectionInfoTimeStatic; } set { RefreshConnectionInfoTimeStatic = value; } }

        public bool Connected
        {
            get
            {
                if (IsDisposed || Client == null)
                    return false;

                var tcpConnections = ConnectedTCPs
                    .Where(x => x.LocalEndPoint.Equals(Client.Client.LocalEndPoint) && x.RemoteEndPoint.Equals(Client.Client.RemoteEndPoint)).ToArray();

                if (tcpConnections.Length > 0)
                {
                    var stateOfConnection = tcpConnections.First().State;

                    return stateOfConnection == TcpState.Established;
                }
                else
                    return false;
            }
        }

        public string IP => !IsDisposed && Client != null ? ((IPEndPoint) Client.Client.RemoteEndPoint).Address.ToString() : "";
        public int DataAvailable => !IsDisposed && Client != null ? Client.Available : 0;


        private TcpClient Client { get; set; }
        private Stream Stream { get; set; }

        private bool IsDisposed { get; set; }


        public NetworkTCPClientWrapperInstance() { }
        internal NetworkTCPClientWrapperInstance(TcpClient tcpClient)
        {
            Client = tcpClient;
            Client.SendTimeout = 5;
            Client.ReceiveTimeout = 5;
            Client.NoDelay = false;
            Stream = Client.GetStream();

            UpdateConnectedTCPs();
        }


        public INetworkTCPClient Connect(string ip, ushort port)
        {
            if (Connected)
                Disconnect();

            Client = new TcpClient(ip, port) { SendTimeout = 5, ReceiveTimeout = 5, NoDelay = false };
            Stream = Client.GetStream();

            UpdateConnectedTCPs();

            return this;
        }
        public INetworkTCPClient Disconnect()
        {
            if (Client.Connected)
                Client.Client.Disconnect(false);
            
            return this;
        }

        public void Send(byte[] bytes, int offset, int count)
        {
            if (IsDisposed)
                return;

            try { Stream.Write(bytes, offset, count); }
            catch (IOException) { Dispose(); }
            catch (SocketException) { Dispose(); }
        }
        public int Receive(byte[] buffer, int offset, int count)
        {
            if (IsDisposed)
                return -1;

            try { return Stream.Read(buffer, offset, count); }
            catch (IOException) { Dispose(); return -1; }
            catch (SocketException) { Dispose(); return -1; }
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
            if(IsDisposed)
                return;

            Disconnect();

            IsDisposed = true;

            Client?.Close();
            Stream?.Dispose();
        }
    }
}
