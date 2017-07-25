using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Octokit;
using Octokit.Internal;

using PokeD.Server.NetCore.Extensions;

namespace PokeD.Server.NetCore.Updater
{
    public static class GitHub
    {
        private static readonly string Host = "github.com";

        private static readonly string ClientHeader = "PokeD.Server.Desktop";
        private static readonly string ClientToken = "MjAxY2M4NDRiYWJiNzI3YjMyMGM0NDkzZjRmMmEyMTcyMTIzZjMzYg==";

        private static readonly string OrgName = "PokeD";
        private static readonly string RepoName = "PokeD.Server.Desktop";


        private static GitHubClient Client => AsyncExtensions.RunSync(async () => await AnonymousHitRateLimit()) ? TokenClient : AnonymousClient;
        private static GitHubClient AnonymousClient { get; } = new GitHubClient(new Connection(new ProductHeaderValue(ClientHeader)));
        private static GitHubClient TokenClient { get; } = new GitHubClient(new Connection(new ProductHeaderValue(ClientHeader), new InMemoryCredentialStore(new Credentials(Encoding.UTF8.GetString(Convert.FromBase64String(ClientToken))))));

        private static WebsiteChecker WebsiteChecker { get; } = new WebsiteChecker(Host);
        public static bool WebsiteIsUp => WebsiteChecker.Check();

        private static IEnumerable<Release> _getAllReleases;
        public static IEnumerable<Release> GetAllReleases
        {
            get
            {
                if (_getAllReleases != null)
                    return _getAllReleases;
                else
                {
                    try { return _getAllReleases = Client.Repository.Release.GetAll(OrgName, RepoName).Result; }
                    catch (Exception) { return new List<Release>(); }
                }
            }
        }

        private static async Task<bool> AnonymousHitRateLimit() => (await AnonymousClient.Miscellaneous.GetRateLimits()).Resources.Core.Remaining <= 0;

        public static void Update()
        {
            _getAllReleases = null;
        }
    }
}