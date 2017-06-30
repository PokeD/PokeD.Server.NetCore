#if !NETCOREAPP2_0
using System.Linq;

using Octokit;

namespace PokeD.Server.NetCore.Extensions
{
    public static class ReleaseExtensions
    {
        public static ReleaseAsset GetUpdateInfo(this Release release) => release.Assets?.SingleOrDefault(asset => asset.Name == "UpdateInfo.yml");
        public static ReleaseAsset GetRelease(this Release release) => release.Assets?.SingleOrDefault(asset => asset.Name == "Release.zip");
    }
}
#endif