using System.IO;

using PokeD.Server.Storage.Files;

namespace PokeD.Server.NetCore
{
    internal static class LogManager
    {
        private static LogFile LogFile { get; } = new LogFile();

        public static void WriteLine(string message)
        {
            lock (LogFile)
            {
                using (var stream = LogFile.Open(PCLExt.FileStorage.FileAccess.ReadAndWrite))
                using (var writer = new StreamWriter(stream))
                {
                    writer.BaseStream.Seek(0, SeekOrigin.End);
                    writer.WriteLine(message);
                    writer.Flush();
                }
            }
        }
    }
}