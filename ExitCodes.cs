using System;

namespace PokeD.Server.Windows
{
    [Flags]
    public enum ExitCodes
    {
        Success = 0,
        UnknownError = 1,
    }
}
