using System;

using PCLStorage;

namespace PokeD.Server.Windows.WrapperInstances
{
    public class FileSystemWrapperInstance : Core.Wrappers.IFileSystem
    {
        public IFolder UsersFolder { get; }
        public IFolder SettingsFolder { get; }
        public IFolder LogFolder { get; }
        
        public FileSystemWrapperInstance()
        {
            var baseDirectory = FileSystem.Current.GetFolderFromPathAsync(AppDomain.CurrentDomain.BaseDirectory).Result;

            UsersFolder     = baseDirectory.CreateFolderAsync("Users", CreationCollisionOption.OpenIfExists).Result;
            SettingsFolder  = baseDirectory.CreateFolderAsync("Settings", CreationCollisionOption.OpenIfExists).Result;
            LogFolder       = baseDirectory.CreateFolderAsync("Logs", CreationCollisionOption.OpenIfExists).Result;
        }
    }
}
