using System;
using System.IO;

using PCLStorage;

using PokeD.Core.Wrappers;

namespace PokeD.Server.Windows
{
    public static class LogManager
    {
        private static IFile LogFile { get; set; }

        static LogManager()
        {
            LogFile = FileSystemWrapper.LogFolder.CreateFileAsync(string.Format("{0:yyyy-MM-dd_hh.mm.ss}", DateTime.Now), CreationCollisionOption.OpenIfExists).Result;
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