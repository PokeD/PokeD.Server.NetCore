using System;

using PCLStorage;

namespace PokeD.Server.Windows.WrapperInstances
{
    public class FileSystemWrapperInstance : Core.Wrappers.IFileSystem
    {
        public IFolder UsersFolder { get; private set; }

        public IFolder SettingsFolder { get; private set; }

        public IFolder LogFolder { get; private set; }


        public FileSystemWrapperInstance()
        {
            var baseDirectory = FileSystem.Current.GetFolderFromPathAsync(AppDomain.CurrentDomain.BaseDirectory).Result;

            UsersFolder     = baseDirectory.CreateFolderAsync("Users", CreationCollisionOption.OpenIfExists).Result;
            SettingsFolder  = baseDirectory.CreateFolderAsync("Settings", CreationCollisionOption.OpenIfExists).Result;
            LogFolder       = baseDirectory.CreateFolderAsync("Logs", CreationCollisionOption.OpenIfExists).Result;
        }
    }
}
