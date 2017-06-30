#if !NETCOREAPP2_0
using System;
using System.IO.Compression;
using System.Linq;
using System.Net;

using Octokit;

using PCLExt.FileStorage;

using PokeD.Server.NetCore.Storage.Files;
using PokeD.Server.NetCore.Storage.Folders;

namespace PokeD.Server.NetCore.Updater
{
    public class DirectUpdater : IDisposable
    {
        private WebClient Downloader { get; set; }
        private bool Cancelled { get; set; }

        private ReleaseAsset ReleaseAsset { get; }
        private IFile TempFile => new TempFile(ReleaseAsset.Name);
        private IFolder ExtractionFolder { get; }

        public DirectUpdater(ReleaseAsset releaseAsset, IFolder extractionFolder)
        {
            ReleaseAsset = releaseAsset;
            ExtractionFolder = extractionFolder;
        }

        public bool Start()
        {
            TempFile.Delete();

            try
            {
                using (Downloader = new WebClient())
                    Downloader.DownloadFile(new Uri(ReleaseAsset.BrowserDownloadUrl), TempFile.Path);
            }
            catch (WebException) { return false; }

            return ExtractFile();
        }
        private bool ExtractFile()
        {
            if (Cancelled) return false;

            using (var fs = TempFile.Open(FileAccess.Read))
            using (var zip = new ZipArchive(fs))
            {
                IFolder root = ExtractionFolder;
                var list = zip.Entries;
                foreach (var zipEntry in list.Skip(1).Where(z => !string.IsNullOrEmpty(z.Name)))
                {
                    if (Cancelled) return false;

                    var path = zipEntry.FullName.Replace(zipEntry.Name, "").TrimEnd('/').TrimEnd('\\');
                    var file = GetFolder(root, path).CreateFile(System.IO.Path.GetFileName(path), CreationCollisionOption.OpenIfExists);
                    using (var stream = file.Open(FileAccess.ReadAndWrite))
                    using (var inputStream = zipEntry.Open())
                    {
                        stream.Position = 0;
                        inputStream.CopyTo(stream);
                    }
                }
            }

            return true;
        }
        private IFolder GetFolder(IFolder root, string path)
        {
            var folders = path.Split('/').Reverse().Skip(1).Reverse();

            IFolder returnFolder = root;
            foreach (var folder in folders)
                returnFolder = returnFolder.CreateFolder(folder, CreationCollisionOption.OpenIfExists);
            return returnFolder;
        }

        public void Dispose()
        {
            Cancelled = true;
            Downloader?.CancelAsync();

            new TempFolder().Delete();
        }
    }
}
#endif