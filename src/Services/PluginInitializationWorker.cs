using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ActivityPub.Services
{
    /// <summary>
    /// Executes required initialization tasks during the Jellyfin server startup sequence.
    /// </summary>
    public class PluginInitializationWorker : IServerEntryPoint
    {
        private readonly ILogger<PluginInitializationWorker> _logger;
        private readonly CryptographyService _cryptoService;

        public PluginInitializationWorker(ILogger<PluginInitializationWorker> logger, CryptographyService cryptoService)
        {
            _logger = logger;
            _cryptoService = cryptoService;
        }

        public Task RunAsync()
        {
            _logger.LogInformation("ActivityPub Plugin Initialization starting...");

            // Trigger the key generation check on startup to ensure keys persist
            // and are available immediately for federation.
            _cryptoService.EnsureKeysExist();

            _logger.LogInformation("ActivityPub Plugin Initialization complete.");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            // No unmanaged resources to clean up.
        }
    }
}
