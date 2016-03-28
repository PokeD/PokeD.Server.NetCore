using System;
using System.IO;

using Aragas.Core.Wrappers;

using PCLStorage;

namespace PokeD.Server.Desktop
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
            lock (LogFile)
            {
                using (var stream = LogFile.OpenAsync(PCLStorage.FileAccess.ReadAndWrite).Result)
                using (var writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    writer.BaseStream.Seek(0, SeekOrigin.End);
                    writer.WriteLine(message);
                }
            }
        }
    }
}