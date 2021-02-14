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
        private readonly ModuleManagerService _moduleManager;

        public RootController(ILogger<RootController> logger, ModuleManagerService moduleManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
        }

        [HttpGet("status")]
        public async Task<ActionResult> StatusAsync(CancellationToken ct)
        {
            var response = new Response();
            _moduleManager.AllClientsForeach(func =>
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