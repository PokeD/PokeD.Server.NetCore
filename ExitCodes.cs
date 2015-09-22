using System;

namespace PokeD.Server.Desktop
{
    [Flags]
    public enum ExitCodes
    {
        Success = 0,
        UnknownError = 1,
    }
}
