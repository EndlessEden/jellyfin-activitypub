using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.ActivityPub.Services;
using MediaBrowser.Controller.Plugins;
using System.Collections.Generic;

namespace Jellyfin.Plugin.ActivityPub.Api
{
    [ApiController]
    [Route("plugins/activitypub/inbox")]
    public class InboxController : ControllerBase
    {
        private readonly ILogger<InboxController> _logger;
        private readonly IngressQueueWorker _queueWorker;

        // We inject IEnumerable<IServerEntryPoint> to safely extract our specific worker 
        // from Jellyfin's standard Dependency Injection container.
        public InboxController(ILogger<InboxController> logger, IEnumerable<IServerEntryPoint> entryPoints)
        {
            _logger = logger;
            _queueWorker = entryPoints.OfType<IngressQueueWorker>().FirstOrDefault() 
                           ?? throw new InvalidOperationException("ActivityPub IngressQueueWorker is not registered.");
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            // 1. Extract the standard HTTP Signature header
            if (!Request.Headers.TryGetValue("Signature", out var signatureValues))
            {
                _logger.LogWarning("ActivityPub Inbox request rejected: Missing Signature header.");
                return Unauthorized("Missing Signature header.");
            }

            var signatureHeader = signatureValues.ToString();
            SignatureVerifier signature;

            try
            {
                // 2. Parse the header components
                signature = SignatureVerifier.Parse(signatureHeader);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ActivityPub Inbox request rejected: Malformed Signature header.");
                return BadRequest("Malformed Signature header.");
            }

            // 3. Extract the raw JSON payload from the request body
            using var reader = new StreamReader(Request.Body);
            var rawJson = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                _logger.LogWarning("ActivityPub Inbox request rejected: Empty request body.");
                return BadRequest("Empty payload.");
            }

            // 4. Drop the payload and signature data into our background FIFO queue
            await _queueWorker.EnqueueAsync(rawJson, signature);

            // 5. Immediately return 202 Accepted, freeing the web thread in milliseconds
            return Accepted();
        }
    }
}
