using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace PokeD.Server.NetCore.Updater
{
    public static class Program
    {
        private static string MainFolderPath { get; } = AppDomain.CurrentDomain.BaseDirectory;

        private const string UpdateFoldername = "Update";
        private static string UpdateFolderPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, UpdateFoldername);

        private const string UpdaterExeFilename = "PokeD.Server.Desktop.Updater.exe";
        private const string ServerExeFilename = "PokeD.Server.Desktop.exe";


        public static void Main(string[] args)
        {
            // -- Wait 1 second so the Server exit.
            Thread.Sleep(1000);

            if (!Directory.Exists(UpdateFolderPath))
                return;

            var files = Directory.GetFiles(UpdateFolderPath);
            if (!files.Any())
                return;


            MoveDirectory(UpdateFolderPath, MainFolderPath);


            // -- Start Server.
            new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = Path.Combine(MainFolderPath, ServerExeFilename),
                    CreateNoWindow = true
                }
            }.Start();
            Environment.Exit(0);
        }

        private static void MoveDirectory(string source, string target)
        {
            var sourcePath = source.TrimEnd('\\', ' ');
            var targetPath = target.TrimEnd('\\', ' ');
            var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories).GroupBy(Path.GetDirectoryName);
            foreach (var folder in files)
            {
                var targetFolder = folder.Key.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(targetFolder);
                foreach (var file in folder)
                {
                    var filename = Path.GetFileName(file);
                    if (filename == UpdaterExeFilename)
                        continue;

                    var targetFile = Path.Combine(targetFolder, filename);
                    if (File.Exists(targetFile))
                        File.Delete(targetFile);
                    File.Move(file, targetFile);
                }
            }
            Directory.Delete(source, true);
        }
    }
}
