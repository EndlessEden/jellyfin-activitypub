using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ActivityPub
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static Plugin Instance { get; private set; }

        private readonly ILogger<Plugin> _logger;
        public string DataFolderPath { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _logger = logger;

            // Establish an isolated path for the ActivityPub SQLite context
            DataFolderPath = Path.Combine(applicationPaths.PluginConfigurationsPath, "ActivityPub");
            Directory.CreateDirectory(DataFolderPath);
        }

        public override string Name => "ActivityPub Federation";

        public override Guid Id => Guid.Parse("A1B2C3D4-E5F6-4a1b-9c8d-7e6f5a4b3c2d"); // Unique ID for this plugin

        public override string Description => "Decentralized, ActivityPub-powered federation for Jellyfin.";

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name,
                    EmbeddedResourcePath = string.Format("{0}.Configuration.configPage.html", GetType().Namespace)
                }
            };
        }
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        // Standard configuration stub. Can be expanded later if settings need to be exposed to the UI.
    }
}
