#if !NETCOREAPP2_0
using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PokeD.Server.NetCore.Updater
{
    internal class WebsiteChecker
    {
        private string Host { get; }
        private bool WebsiteIsUp { get; set; }
        private DateTime LastCheck { get; set; }

        public WebsiteChecker(string host)
        {
            Host = host;
            NetworkChange.NetworkAddressChanged += (s, e) => Check(5000, true);
        }

        public bool Check(int timeout = 2000, bool force = false)
        {
            if (force || DateTime.UtcNow - LastCheck > TimeSpan.FromSeconds(30))
            {
                LastCheck = DateTime.UtcNow;
                try { return WebsiteIsUp = new TcpClient().ConnectAsync(Host, 80).Wait(timeout); }
                catch (Exception e) when (IsSocketException(e)) { return WebsiteIsUp = false; }
            }
            else
                return WebsiteIsUp;
        }
        private bool IsSocketException(Exception e) => e is SocketException || e.InnerException is SocketException;
    }
}
#endif