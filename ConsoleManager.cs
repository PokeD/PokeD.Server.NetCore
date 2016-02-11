using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace PokeD.Server.Desktop
{
    public static class ConsoleManager
    {
        private static Thread ConsoleManagerThread { get; set; }

        public static int ScreenFPS { get { return _screenFPS; } set { _screenFPS = value; _excecutionMilliseconds = 1000 / _screenFPS; UpdateTitle(); } }
        private static int _screenFPS;

        private static int ExcecutionMilliseconds { get { return _excecutionMilliseconds; } set { _excecutionMilliseconds = value; _screenFPS = 1000 / _excecutionMilliseconds; UpdateTitle(); } }
        private static int _excecutionMilliseconds;
        
        private static char[][] ScreenBuffer { get; set; } = new char[0][];
        private static int ScreenWidth => Console.WindowWidth - 1;
        private static int ScreenHeight => Console.WindowHeight - 1;

        private static List<string> ConsoleOutput { get; } = new List<string>();
        private static int ConsoleOutputLength => ScreenHeight - 6 - 2;
        private static Queue<string> ConsoleInput { get; } = new Queue<string>();
        private static string CurrentConsoleInput { get; set; } = string.Empty;
        private static int CurrentLine { get; set; }

        public static bool InputAvailable => !Stopped && ConsoleInput.Count > 0;

        private static bool Stopped { get; set; }



        public static void WriteLine(string text = "")
        {
            if(!Stopped)
                ConsoleOutput.Add(text);
        }
        public static string ReadLine() => Stopped ? string.Empty : ConsoleInput.Dequeue();


        public static void Start(int fps = 20, bool cursorVisible = false)
        {
            if (ConsoleManagerThread != null && ConsoleManagerThread.IsAlive)
                Stop();

            ScreenFPS = fps;
            Console.CursorVisible = cursorVisible;

            ConsoleManagerThread = new Thread(Cycle) { IsBackground = true, Name = "ConsoleManagerThread" };
            ConsoleManagerThread.Start();
        }
        public static void Stop()
        {
            Stopped = true;
            while (ConsoleManagerThread != null && ConsoleManagerThread.IsAlive)
                Thread.Sleep(ExcecutionMilliseconds);
        }


        private static long ConsoleManagerThreadTime { get; set; }
        private static void Cycle()
        {
            var watch = Stopwatch.StartNew();
            while (!Stopped)
            {
                if (ScreenBuffer.Length != ScreenHeight)
                {
                    ScreenBuffer = new char[ScreenHeight][];
                    for (var y = 0; y < ScreenBuffer.Length; y++)
                        ScreenBuffer[y] = new char[ScreenWidth];
                }

                var emptyLine = string.Empty.PadRight(ScreenWidth).ToCharArray();
                for (var cy = 0; cy < ScreenHeight; cy++)
                    ScreenBuffer[cy] = emptyLine;
                CurrentLine = 0;

                DrawLine($"Main              thread execution time: {Program.MainThreadTime} ms");
                DrawLine($"ClientConnections thread execution time: {Server.ClientConnectionsThreadTime} ms");
                DrawLine($"PlayerWatcher     thread execution time: {ModuleP3D.PlayerWatcherThreadTime} ms");
                DrawLine($"PlayerCorrection  thread execution time: {ModuleP3D.PlayerCorrectionThreadTime} ms");
                DrawLine($"ConsoleManager    thread execution time: {ConsoleManagerThreadTime} ms");

                DrawLine();

                for (var i = 0; i < ConsoleOutput.Count; i++)
                    DrawLine(ConsoleOutput[i]);
                
                HandleInput();
                DrawLine(CurrentConsoleInput, ScreenHeight > 0 ? ScreenHeight - 1 : ScreenHeight);


                DrawScreen();


                if (watch.ElapsedMilliseconds < ExcecutionMilliseconds)
                {
                    ConsoleManagerThreadTime = watch.ElapsedMilliseconds;

                    var time = (int)(ExcecutionMilliseconds - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    Thread.Sleep(time);
                }
                watch.Reset();
                watch.Start();
            }

            Console.Clear();
        }

        private static void HandleInput()
        {
            if (!Console.KeyAvailable)
                return;

            var input = Console.ReadKey();
            switch (input.Key)
            {
                case ConsoleKey.Enter:
                    ConsoleOutput.Add(CurrentConsoleInput);
                    if(ConsoleOutput.Count > ConsoleOutputLength)
                        ConsoleOutput.RemoveAt(0);

                    ConsoleInput.Enqueue(CurrentConsoleInput);
                    CurrentConsoleInput = string.Empty;
                    break;

                case ConsoleKey.Backspace:
                    if (CurrentConsoleInput.Length >= 1)
                        CurrentConsoleInput = CurrentConsoleInput.Remove(CurrentConsoleInput.Length - 1);
                    break;

                case ConsoleKey.Escape:
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                case ConsoleKey.LeftArrow:
                case ConsoleKey.RightArrow:
                case ConsoleKey.Tab:
                case ConsoleKey.Delete:
                    break;

                default:
                    CurrentConsoleInput += input.KeyChar;
                    break;
            }
        }

        private static void DrawLine(string text = "")
        {
            if (text.Length > ScreenWidth)
            {
                DrawLineInternal(text.Substring(0, ScreenWidth));
                DrawLine(text.Remove(0, ScreenWidth));
            }
            else
                DrawLineInternal(text);
        }
        private static void DrawLine(string text, int y)
        {
            if (text.Length > ScreenWidth)
                DrawLineInternal(text.Substring(0, ScreenWidth), y);
            else
                DrawLineInternal(text, y);
        }
        private static void DrawLineInternal(string text)
        {
            if (ScreenBuffer.Length > CurrentLine)
                ScreenBuffer[CurrentLine] = text.PadRight(ScreenWidth).ToCharArray();

            CurrentLine++;
        }
        private static void DrawLineInternal(string text, int y)
        {
            if (ScreenBuffer.Length > y)
                ScreenBuffer[y] = text.PadRight(ScreenWidth).ToCharArray();
        }
        private static void DrawScreen()
        {
            Console.SetCursorPosition(0, 0);
            for (var y = 0; y < ScreenHeight; ++y)
                Console.WriteLine(ScreenBuffer[y]);
        }

        private static void UpdateTitle() { Console.Title = $"PokeD Server FPS: {ScreenFPS}"; }

        public static void Clear()
        {
            if (!Stopped)
                ConsoleOutput.Clear();
        }
    }
}