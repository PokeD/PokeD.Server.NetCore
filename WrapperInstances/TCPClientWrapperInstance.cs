using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class TCPClientClass1 : ITCPClient
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


        public TCPClientClass1() { }
        internal TCPClientClass1(TcpClient tcpClient)
        {
            Client = tcpClient;
            Client.SendTimeout = 5;
            Client.ReceiveTimeout = 5;
            Client.NoDelay = false;
            Stream = Client.GetStream();

            UpdateConnectedTCPs();
        }


        public ITCPClient Connect(string ip, ushort port)
        {
            if (Connected)
                Disconnect();

            Client = new TcpClient(ip, port) { SendTimeout = 5, ReceiveTimeout = 5, NoDelay = false };
            Stream = Client.GetStream();

            UpdateConnectedTCPs();

            return this;
        }
        public ITCPClient Disconnect()
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

        public void WriteByteArray(byte[] array)
        {
            
        }

        public byte[] ReadByteArray(int length)
        {
            return null;
        }

        public Stream GetStream()
        {
            return Stream;
        }

        public ITCPClientWrapper NewInstance()
        {
            return new TCPClientWrapperInstance();
        }


        public void Dispose()
        {
            if (IsDisposed)
                return;

            Disconnect();

            IsDisposed = true;

            Client?.Close();
            Stream?.Dispose();
        }
    }

    public class TCPClientClass : ITCPClient
    {
        public int RefreshConnectionInfoTime { get; set; }

        public string IP => !IsDisposed && Client != null && Client.Client.Connected ? ((IPEndPoint) Client.Client.RemoteEndPoint).Address.ToString() : "";
        public bool Connected => !IsDisposed && Client != null && Client.Client.Connected;
        public int DataAvailable => !IsDisposed && Client != null ? Client.Available : 0;

        private TcpClient Client { get; set; }
        private Stream Stream { get; set; }

        private bool IsDisposed { get; set; }


        public TCPClientClass() { }
        public TCPClientClass(TcpClient tcpClient)
        {
            Client = tcpClient;
            Client.SendTimeout = 5;
            Client.ReceiveTimeout = 5;
            Client.NoDelay = false;
            Stream = Client.GetStream();

        }
        
        public ITCPClient Connect(string ip, ushort port)
        {
            if (Connected)
                Disconnect();

            Client = new TcpClient(ip, port) { SendTimeout = 5, ReceiveTimeout = 5, NoDelay = false };
            Stream = Client.GetStream();

            return this;
        }
        public ITCPClient Disconnect()
        {
            if (Connected)
                Client.Client.Disconnect(false);

            return this;
        }

        public void WriteByteArray(byte[] array)
        {
            if (IsDisposed)
                return;

            try
            {
                var length = array.Length;
                var buffer = length < Client.SendBufferSize ?
                    new byte[length] : 
                    new byte[Client.ReceiveBufferSize];
                
                var totalWritedLength = 0;
                using (var data = new MemoryStream(array))
                {
                    do
                    {
                        var writedLength = data.Read(buffer, 0, buffer.Length);
                        Stream.Write(buffer, 0, buffer.Length);
                        totalWritedLength += writedLength;
                    } while (totalWritedLength < length);

                    if(length >= 46857)
                        Logger.Log(LogType.GlobalError, $"TCPClientClass: WriteByteArray: length >= 46857; Length - {length}, totalWri - {totalWritedLength}, data.Length - {data.Length}");
                }
            }
            catch (IOException) { Dispose(); }
            catch (SocketException) { Dispose(); }
        }
        public byte[] ReadByteArray(int length)
        {
            if (IsDisposed)
                return new byte[0];

            try
            {
                var buffer = length < Client.ReceiveBufferSize ?
                    new byte[length] :
                    new byte[Client.ReceiveBufferSize];

                var totalNumberOfBytesRead = 0;
                using (var receivedData = new MemoryStream())
                {
                    do
                    {
                        var numberOfBytesRead = Stream.Read(buffer, 0, buffer.Length);
                        if (numberOfBytesRead == 0)
                        {
                            Logger.Log(LogType.GlobalError, $"TCPClientClass: ReadByteArray: numberOfBytesRead == 0; Length - {length}, totalRec - {totalNumberOfBytesRead}, stream.Length - {receivedData.Length}");
                            break;
                        }

                        receivedData.Write(buffer, 0, buffer.Length); //Write to memory stream
                        totalNumberOfBytesRead += numberOfBytesRead;
                    } while (totalNumberOfBytesRead < length);

                    return receivedData.ToArray();
                }
            }
            catch (IOException) { Dispose(); return new byte[0]; }
            catch (SocketException) { Dispose(); return new byte[0]; }
        }

        public Stream GetStream() { return Stream; }

        public void Dispose()
        {
            if (Connected)
                Disconnect();

            IsDisposed = true;

            Client?.Close();
            Stream?.Dispose();
        }
    }

    public class TCPClientWrapperInstance : ITCPClientWrapper
    {
        public ITCPClient CreateTCPClient() { return new TCPClientClass(); }
    }
}
