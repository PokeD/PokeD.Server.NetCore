using System;

using PokeD.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class InputWrapperInstance : IInputWrapper
    {
        public event Action<string> OnKey;

        public InputWrapperInstance() { }

        public void ShowKeyboard() { }

        public void HideKeyboard() { }

        public void ConsoleWrite(string message)
        {
            ConsoleManager.WriteLine(message);
        }

        public void LogWriteLine(string message)
        {
            LogManager.WriteLine(message);
        }
    }
}
