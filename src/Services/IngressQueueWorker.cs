using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ActivityPub.Services
{
    /// <summary>
    /// Jellyfin native background worker. Starts automatically on server boot.
    /// </summary>
    public class IngressQueueWorker : IServerEntryPoint
    {
        private readonly ILogger<IngressQueueWorker> _logger;
        private readonly Channel<InboxPayload> _queue;

        public IngressQueueWorker(ILogger<IngressQueueWorker> logger)
        {
            _logger = logger;
            
            // Create an unbounded thread-safe FIFO channel
            _queue = Channel.CreateUnbounded<InboxPayload>();
        }

        /// <summary>
        /// Fired by Jellyfin when the plugin is fully loaded and the server is running.
        /// </summary>
        public Task RunAsync()
        {
            _logger.LogInformation("ActivityPub Ingress Queue Worker starting...");
            
            // Fire and forget the consumption loop on a background thread
            _ = Task.Run(ProcessQueueAsync);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired by Jellyfin during server shutdown.
        /// </summary>
        public void Dispose()
        {
            _queue.Writer.Complete();
        }

        /// <summary>
        /// Called by the InboxController to quickly drop payloads into the background queue.
        /// </summary>
        public async ValueTask EnqueueAsync(string rawJson, SignatureVerifier signature)
        {
            await _queue.Writer.WriteAsync(new InboxPayload 
            { 
                RawJson = rawJson, 
                Signature = signature 
            });
        }

        /// <summary>
        /// The background loop that drains the queue sequentially.
        /// </summary>
        private async Task ProcessQueueAsync()
        {
            try
            {
                // ReadAllAsync blocks until a new item is available, draining the queue efficiently
                await foreach (var payload in _queue.Reader.ReadAllAsync())
                {
                    _logger.LogInformation("Processing queued ActivityPub payload from Actor KeyId: {KeyId}", payload.Signature.KeyId);
                    
                    // TODO (Phase 1.2): 
                    // 1. Fetch public key using payload.Signature.KeyId via HttpClient
                    // 2. Cryptographically verify the payload against the signature
                    // 3. Process the ActivityPub JSON (Follow, Accept, Update, etc.)
                    
                    // Yield to prevent thread starvation during heavy bursts
                    await Task.Yield(); 
                }
            }
            catch (ChannelClosedException)
            {
                _logger.LogInformation("ActivityPub Ingress Queue channel closed safely.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A fatal error occurred in the ActivityPub Ingress Queue loop.");
            }
        }
    }

    /// <summary>
    /// Data structure representing a queued inbox item.
    /// </summary>
    public class InboxPayload
    {
        public string RawJson { get; set; }
        public SignatureVerifier Signature { get; set; }
    }
}
