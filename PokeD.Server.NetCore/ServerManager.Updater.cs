using System;
using System.Linq;
using System.Reflection;
using System.Threading;

using PokeD.Server.NetCore.Extensions;
using PokeD.Server.NetCore.Storage.Files;
using PokeD.Server.NetCore.Storage.Folders;
using PokeD.Server.NetCore.Updater;

namespace PokeD.Server.NetCore
{
    public partial class ServerManager
    {
        private static readonly string UpToDate = "Server is up to date!";
        private static readonly string UpdateAvailable = "A new Server version is available!\nWould you like to update?";
        private static readonly string UpdateNotFound = "No update was found!";

        private void CheckServerForUpdate()
        {
            if(DisableUpdate)
                return;

            var launcherReleases = GitHub.GetAllReleases.ToList();

            if (launcherReleases.Any())
            {
                var latestRelease = launcherReleases.First();
                if (Assembly.GetExecutingAssembly().GetName().Version > new Version(latestRelease.TagName))
                {
                    Console.WriteLine(UpdateAvailable);

                    string response;
                    while (string.IsNullOrEmpty(response = Console.ReadLine().ToLower()))
                        Thread.Sleep(50);
                    
                    if (response == "yes" || response == "ye" || response == "y" || response == "yup")
                    {
                        using (var directUpdater = new DirectUpdater(latestRelease.GetRelease(), new UpdateFolder()))
                            directUpdater.Start();

                        new UpdaterFile().Start(createNoWindow: true);
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