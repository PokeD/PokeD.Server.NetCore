using System;

using PCLStorage;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class FileSystemWrapperInstance : Aragas.Core.Wrappers.IFileSystem
    {
        public IFolder SettingsFolder { get; }
        public IFolder LogFolder { get; }
        public IFolder CrashLogFolder { get; }
        public IFolder LuaFolder { get; }
        public IFolder AssemblyFolder { get; }
        public IFolder DatabaseFolder { get; }
        public IFolder ContentFolder { get; }
        public IFolder OutputFolder { get; }

        public FileSystemWrapperInstance()
        {
            var baseDirectory = FileSystem.Current.GetFolderFromPathAsync(AppDomain.CurrentDomain.BaseDirectory).Result;

            SettingsFolder  = baseDirectory.CreateFolderAsync("Settings", CreationCollisionOption.OpenIfExists).Result;
            LogFolder       = baseDirectory.CreateFolderAsync("Logs", CreationCollisionOption.OpenIfExists).Result;
            CrashLogFolder  = LogFolder.CreateFolderAsync("Crash", CreationCollisionOption.OpenIfExists).Result;
            LuaFolder       = baseDirectory.CreateFolderAsync("Lua", CreationCollisionOption.OpenIfExists).Result;
            DatabaseFolder  = baseDirectory.CreateFolderAsync("Database", CreationCollisionOption.OpenIfExists).Result;
            ContentFolder   = baseDirectory.CreateFolderAsync("Content", CreationCollisionOption.OpenIfExists).Result;
        }
    }
}
