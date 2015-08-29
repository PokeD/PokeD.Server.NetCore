using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using PokeD.Core.Data;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Wrappers;
using PokeD.Server.Data;
using PokeD.Server.Windows.WrapperInstances;

namespace PokeD.Server.Windows
{
    public static class ConsoleManager
    {
        public static int ConsoleManagerThreadTime { get; set; }

        private static Thread ConsoleManagerThread { get; set; }

        private static char[,] ScreenBufferArray { get; set; }
        private static string ScreenBuffer { get; set; }
        private static int ScreenWidth { get { return ScreenBufferArray.GetLength(0); } }
        private static int ScreenHeight { get { return ScreenBufferArray.GetLength(1); } }

        public static int ScreenFPS { get { return _screenFPS; } set { _screenFPS = value; _excecutionMilliseconds = 1000 / _screenFPS; UpdateTitle(); } }
        private static int _screenFPS;

        private static int ExcecutionMilliseconds { get { return _excecutionMilliseconds; } set { _excecutionMilliseconds = value; _screenFPS = 1000 / _excecutionMilliseconds; UpdateTitle(); } }
        private static int _excecutionMilliseconds;

        private static List<string> ConsoleOutput { get; set; }


        public static void WriteLine(string text)
        {
            ConsoleOutput.Add(text);
        }

        public static void Start()
        {
            if (ConsoleManagerThread == null || !ConsoleManagerThread.IsAlive)
            {
                ScreenBufferArray = new char[Console.WindowWidth, Console.WindowHeight];
                ConsoleOutput = new List<string>();
                ScreenFPS = 60;

                Console.CursorVisible = true;

                UpdateTitle();

                ConsoleManagerThread = new Thread(Cycle) {IsBackground = true, Name = "ConsoleManagerThread"};
                ConsoleManagerThread.Start();
            }
        }

        
        private static void Cycle()
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                ScreenBufferArray = new char[Console.WindowWidth, Console.WindowHeight];
                ScreenBuffer = string.Empty;

                DrawLine(string.Format("Main           thread execution time: {0} ms", Program.MainThreadTime        ), 0);
                DrawLine(string.Format("ClientListner  thread execution time: {0} ms", Server.ClientListnerThreadTime), 1);
                DrawLine(string.Format("WorldProcessor thread execution time: {0} ms", World.WorldProcessorThreadTime), 2);
                DrawLine(string.Format("PlayerWatcher  thread execution time: {0} ms", Server.PlayerWatcherThreadTime), 3);
                DrawLine(string.Format("ConsoleManager thread execution time: {0} ms", ConsoleManagerThreadTime      ), 4);

                var currentLineCursor = 6;
                for (int i = ConsoleOutput.Count - 1; i >= 0 && currentLineCursor < Console.WindowHeight; i--)
                    Draw(ConsoleOutput[i], 0, currentLineCursor++);
                
                DrawScreen();


                ConsoleManagerThreadTime = (int) watch.ElapsedMilliseconds;
                Thread.Sleep(ExcecutionMilliseconds);
                watch.Reset();
                watch.Start();
            }
        }


        private static void Draw(string text, int x, int y)
        {
            var count = 0;
            for (var i = 0; i < text.Length && ScreenWidth > x + i && ScreenHeight > y; i++)
                ScreenBufferArray[x + count++, y] = text[i];
        }

        private static void DrawLine(string text, int y)
        {
            var count = 0;
            for (var i = 0; i < text.Length && ScreenWidth > y; i++)
                ScreenBufferArray[count++, y] = text[i];
        }

        private static void DrawScreen()
        {
            for (var iy = 0; iy < ScreenHeight - 1; iy++)
                for (var ix = 0; ix < ScreenWidth; ix++)
                    ScreenBuffer += ScreenBufferArray[ix, iy];

            Console.SetCursorPosition(0, 0);
            Console.Write(ScreenBuffer);
        }

        private static void UpdateTitle()
        {
            Console.Title = string.Format("PokeD Server FPS: {0}", ScreenFPS);
        }
    }

    public static class Program
    {
        public static int MainThreadTime { get; set; }

        static Program()
        {
            FileSystemWrapper.Instance = new FileSystemWrapperInstance();
            NetworkTCPClientWrapper.Instance = new NetworkTCPClientWrapperInstance();
            NetworkTCPServerWrapper.Instance = new NetworkTCPServerWrapperInstance();
            InputWrapper.Instance = new InputWrapperInstance();
            ThreadWrapper.Instance = new ThreadWrapperInstance();
        }

        public static Server Server { get; set; }

        public static void Main(string[] args)
        {
            Server = new Server();

            ConsoleManager.Start();

            Update();
        }

        public static void Update()
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var input = Console.ReadLine();
                    if (input.StartsWith("/say"))
                        Server.SendToAllPlayers(new ChatMessagePacket { DataItems = new DataItems(input.Replace("/say ", "")) });
                }


                Server.Update();

                if (watch.ElapsedMilliseconds < 10)
                {
                    var time = (int) (10 - watch.ElapsedMilliseconds);
                    if (time < 0)
                        time = 0;

                    MainThreadTime = (int) watch.ElapsedMilliseconds;
                    Thread.Sleep(time);
                }

                watch.Reset();
                watch.Start();
            }
        }
    }
}
