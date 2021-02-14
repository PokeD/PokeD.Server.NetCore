namespace PokeD.Server.NetCore
{
    public record ServerManagerOptions
    {
        public bool NATForwardingEnabled { get; init; }
        public bool DisableUpdate { get; init; }
    }

    public record ServerOptions
    {
        public string PokeApiUrl { get; init; } = "https://pokeapi.co/";
    }
}