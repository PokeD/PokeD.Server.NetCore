using System;
using System.IO;

using PCLStorage;

using PokeD.Core.Wrappers;

namespace PokeD.Server.Windows
{
    public static class LogManager
    {
        private static IFile LogFile { get; }

        static LogManager()
        {
            LogFile = FileSystemWrapper.LogFolder.CreateFileAsync($"{DateTime.Now:yyyy-MM-dd_HH.mm.ss}.log", CreationCollisionOption.OpenIfExists).Result;
        }

        public static void WriteLine(string message)
        {
            using (var stream = LogFile.OpenAsync(PCLStorage.FileAccess.ReadAndWrite).Result)
            using (var writer = new StreamWriter(stream) {AutoFlush = true})
            {
                writer.BaseStream.Seek(0, SeekOrigin.End);
                writer.WriteLine(message);
            }
        }
    }
}