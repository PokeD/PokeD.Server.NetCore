using System.Linq;
using System.Runtime.InteropServices;

using Octokit;

namespace PokeD.Server.NetCore.Extensions
{
    public static class ReleaseExtensions
    {
        public static ReleaseAsset GetReleaseAsset(this Release release)
        {
            var runtimeIdentifier = string.Empty;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                runtimeIdentifier += "win";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                runtimeIdentifier += "linux";
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                runtimeIdentifier += "osx";

            switch (RuntimeInformation.OSArchitecture)
            {
                case Architecture.X86:
                    runtimeIdentifier += "-x86";
                    break;
                case Architecture.X64:
                    runtimeIdentifier += "-x64";
                    break;
                case Architecture.Arm:
                    runtimeIdentifier += "-arm";
                    break;
                case Architecture.Arm64:
                    runtimeIdentifier += "-arm";
                    break;
            }

            return release.Assets?.SingleOrDefault(asset => asset.Name == $"{runtimeIdentifier}.zip");
        }
    }
}