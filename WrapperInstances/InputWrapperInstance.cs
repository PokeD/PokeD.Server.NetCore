using System;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class InputWrapperInstance : IInputWrapper
    {
        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        public InputWrapperInstance() { }

        public void ShowKeyboard() { }

        public void HideKeyboard() { }

        public void ConsoleWrite(string message) { ConsoleManager.WriteLine(message); }

        public void LogWriteLine(DateTime time, string message)
        {
            var msg_0 = $"[{DateTime.Now:yyyy-MM-dd_HH:mm:ss}]_{message}";
            LogManager.WriteLine(msg_0);

            var msg_1 = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            ConsoleManager.WriteLine(msg_1);
        }
    }
}
