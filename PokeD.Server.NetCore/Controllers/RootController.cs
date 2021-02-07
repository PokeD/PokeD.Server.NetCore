using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using PokeD.Server.Clients.P3D;
using PokeD.Server.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PokeD.Server.NetCore.Controllers
{
    public class Player
    {
        public string Name { get; set; }
        public string GameJoltId { get; set; }
    }
    public class Response
    {
        public List<Player> Players { get; set; } = new List<Player>();
    }

    [ApiController]
    [Route("/")]
    public class RootController : ControllerBase
    {
        private readonly ILogger<RootController> _logger;
        private readonly ServerManager _serverManager;

        public RootController(ILogger<RootController> logger, ServerManager serverManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serverManager = serverManager ?? throw new ArgumentNullException(nameof(serverManager));
        }

        [HttpGet("status")]
        public async Task<ActionResult> StatusAsync(CancellationToken ct)
        {
            var moduleManager = _serverManager.Server.Services.GetService<ModuleManagerService>();
            var response = new Response();
            moduleManager.AllClientsForeach(func =>
            {
                foreach (var client in func)
                {
                    if (client is P3DPlayer p3dPlayer)
                    {
                        response.Players.Add(new Player { Name =  p3dPlayer.Nickname, GameJoltId = p3dPlayer.GameJoltID.ToString()});
                    }
                }
            });
            return Ok(response);
        }
    }
}