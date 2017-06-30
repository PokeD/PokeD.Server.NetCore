#if !NETCOREAPP2_0
using System;

using Octokit;

using PokeD.Server.NetCore.Extensions;

namespace PokeD.Server.NetCore.Updater
{
    internal class GitHubRelease
    {
        private Release Release { get; }
        public ReleaseAsset ReleaseAsset => Release.GetRelease();
        public ReleaseAsset UpdateInfoAsset => Release.GetUpdateInfo();
        public Version Version => Version.TryParse(Release.TagName, out var version) ? version : new Version("0.0");
        public DateTime ReleaseDate => Release.CreatedAt.DateTime;

        public GitHubRelease(Release release) { Release = release; }
    }
}
#endif