using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace PokeD.Server.Windows
{
    public static class ConsoleManager
    {
        private static Thread ConsoleManagerThread { get; set; }

        private static char[,] ScreenBufferArray { get; set; }
        private static string ScreenBuffer { get; set; }
        private static int ScreenWidth => ScreenBufferArray.GetLength(0);
        private static int ScreenHeight => ScreenBufferArray.GetLength(1);

        public static int ScreenFPS { get { return _screenFPS; } set { _screenFPS = value; _excecutionMilliseconds = 1000 / _screenFPS; UpdateTitle(); } }
        private static int _screenFPS;

        private static int ExcecutionMilliseconds { get { return _excecutionMilliseconds; } set { _excecutionMilliseconds = value; _screenFPS = 1000 / _excecutionMilliseconds; UpdateTitle(); } }
        private static int _excecutionMilliseconds;

        private static List<string> ConsoleOutput { get; }
        private static Queue<string> ConsoleInput { get; }
        private static string CurrentConsoleInput { get; set; }

        public static bool InputAvailable => ConsoleInput.Count > 0;

        static ConsoleManager()
        {
            ConsoleOutput = new List<string>();
            ConsoleInput = new Queue<string>();
            CurrentConsoleInput = string.Empty;
        }

        public static void WriteLine(string text)
        {
            ConsoleOutput.Add(text);
        }

        public static string ReadLine()
        {
            return ConsoleInput.Dequeue();
        }

        public static void Start()
        {
            if (ConsoleManagerThread != null && ConsoleManagerThread.IsAlive)
                return;

            ScreenBufferArray = new char[Console.WindowWidth, Console.WindowHeight];
            ScreenFPS = 60;

            Console.CursorVisible = true;

            UpdateTitle();

            ConsoleManagerThread = new Thread(Cycle) { IsBackground = true, Name = "ConsoleManagerThread" };
            ConsoleManagerThread.Start();
        }


        private static long ConsoleManagerThreadTime { get; set; }
        private static void Cycle()
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                ScreenBufferArray = new char[Console.WindowWidth, Console.WindowHeight];
                ScreenBuffer = string.Empty;

                DrawLine($"Main              thread execution time: {Program.MainThreadTime} ms",               0);
                DrawLine($"ClientConnections thread execution time: {Server.ClientConnectionsThreadTime} ms",   1);
                DrawLine($"PlayerWatcher     thread execution time: {Server.PlayerWatcherThreadTime} ms",       2);
                DrawLine($"PlayerCorrection  thread execution time: {Server.PlayerCorrectionThreadTime} ms",    3);
                DrawLine($"ConsoleManager    thread execution time: {ConsoleManagerThreadTime} ms",             4);

                var currentLineCursor = 6;
                for (var i = 0; i < ConsoleOutput.Count && currentLineCursor < Console.WindowHeight - 2; i++)
                    Draw(ConsoleOutput[i], 0, currentLineCursor++);

                HandleInput();
                DrawLine(CurrentConsoleInput, Console.WindowHeight - 2);


                DrawScreen();


                if (watch.ElapsedMilliseconds < ExcecutionMilliseconds)
                {
                    ConsoleManagerThreadTime = watch.ElapsedMilliseconds;

                    var time = (int) (ExcecutionMilliseconds - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    Thread.Sleep(time);
                }
                watch.Reset();
                watch.Start();
            }
        }

        private static void HandleInput()
        {
            if (!Console.KeyAvailable)
                return;

            var input = Console.ReadKey();
            switch (input.Key)
            {
                case ConsoleKey.Enter:
                    ConsoleInput.Enqueue(CurrentConsoleInput);
                    CurrentConsoleInput = string.Empty;
                    break;

                case ConsoleKey.Backspace:
                    if (CurrentConsoleInput.Length >= 1)
                        CurrentConsoleInput = CurrentConsoleInput.Remove(CurrentConsoleInput.Length - 1);
                    break;

                case ConsoleKey.Escape:
                case ConsoleKey.LeftArrow:
                case ConsoleKey.RightArrow:
                case ConsoleKey.Home:
                case ConsoleKey.End:
                case ConsoleKey.Delete:
                case ConsoleKey.Oem6:
                case ConsoleKey.DownArrow:
                case ConsoleKey.UpArrow:
                case ConsoleKey.Tab:
                    break;

                default:
                    CurrentConsoleInput += input.KeyChar;
                    break;
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
            Console.Title = $"PokeD Server FPS: {ScreenFPS}";
        }

        public static void Clear()
        {
            ConsoleOutput.Clear();
        }
    }
}