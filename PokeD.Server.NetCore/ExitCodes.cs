using System;

namespace PokeD.Server.NetCore
{
    [Flags]
    public enum ExitCodes
    {
        Success         = 0,
        UnknownError    = 1,
    }
}