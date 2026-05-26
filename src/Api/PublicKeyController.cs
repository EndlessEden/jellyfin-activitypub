using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.ActivityPub.Services;

namespace Jellyfin.Plugin.ActivityPub.Api
{
    [ApiController]
    [Route("plugins/activitypub/key")]
    public class PublicKeyController : ControllerBase
    {
        private readonly ILogger<PublicKeyController> _logger;
        private readonly CryptographyService _cryptoService;

        public PublicKeyController(ILogger<PublicKeyController> logger, CryptographyService cryptoService)
        {
            _logger = logger;
            _cryptoService = cryptoService;
        }

        [HttpGet]
        public IActionResult GetPublicKey()
        {
            try
            {
                var publicKeyPem = _cryptoService.GetPublicKeyPem();
                
                // Return as plain text so remote servers can easily parse the PEM block
                return Content(publicKeyPem, "text/plain");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Public key requested before generation.");
                return NotFound("Public key has not been generated yet.");
            }
        }
    }
}

