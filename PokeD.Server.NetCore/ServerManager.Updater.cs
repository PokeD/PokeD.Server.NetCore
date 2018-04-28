using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Octokit;
using PokeD.Server.NetCore.Extensions;
using PokeD.Server.NetCore.Updater;

namespace PokeD.Server.NetCore
{
    public partial class ServerManager
    {
        private static readonly string WebsiteNotUp = "Could not connect to GitHub.com!";
        private static readonly string UpToDate = "Server is up to date!";
        private static readonly string UpdateAvailable = "A new Server version is available!\nWould you like to download it?";
        private static readonly string UpdateNotFound = "No update was found!";

        private void CheckServerForUpdate()
        {
            if(DisableUpdate)
                return;

            if (!GitHub.WebsiteIsUp)
                Console.WriteLine(WebsiteNotUp);

            var launcherReleases = GitHub.GetAllReleases.ToList();

            if (launcherReleases.Any())
            {
                var latestRelease = launcherReleases.First();
                var latestReleaseAsset = default(ReleaseAsset);
                if (Assembly.GetExecutingAssembly().GetName().Version > new Version(latestRelease.TagName) && ((latestReleaseAsset = latestRelease.GetReleaseAsset()) != null))
                {
                    Console.WriteLine(UpdateAvailable);

                    string response;
                    while (string.IsNullOrEmpty(response = Console.ReadLine().ToLower()))
                        Thread.Sleep(50);
                    
                    if (response == "yes" || response == "ye" || response == "y" || response == "yup" || response == "yar")
                    {
                        Process.Start(latestReleaseAsset.Url);

                        Environment.Exit((int) ExitCodes.Success);
                    }
                }
                else
                    Console.WriteLine(UpToDate);
            }
            else
                Console.WriteLine(UpdateNotFound);
        }
    }
}