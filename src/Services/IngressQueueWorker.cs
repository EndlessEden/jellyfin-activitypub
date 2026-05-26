using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ActivityPub.Services
{
    public class IngressQueueWorker : IServerEntryPoint
    {
        private readonly ILogger<IngressQueueWorker> _logger;
        private readonly Channel<InboxPayload> _queue;
        private readonly IHttpClientFactory _httpClientFactory;

        public IngressQueueWorker(ILogger<IngressQueueWorker> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _queue = Channel.CreateUnbounded<InboxPayload>();
        }

        public Task RunAsync()
        {
            _logger.LogInformation("ActivityPub Ingress Queue Worker starting...");
            _ = Task.Run(ProcessQueueAsync);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _queue.Writer.Complete();
        }

        public async ValueTask EnqueueAsync(string rawJson, SignatureVerifier signature)
        {
            await _queue.Writer.WriteAsync(new InboxPayload 
            { 
                RawJson = rawJson, 
                Signature = signature 
            });
        }

        private async Task ProcessQueueAsync()
        {
            try
            {
                await foreach (var payload in _queue.Reader.ReadAllAsync())
                {
                    _logger.LogInformation("Processing queued ActivityPub payload from Actor KeyId: {KeyId}", payload.Signature.KeyId);
                    
                    bool isVerified = await VerifySignatureAsync(payload);

                    if (isVerified)
                    {
                        _logger.LogInformation("Cryptographic verification SUCCESS for payload from {KeyId}.", payload.Signature.KeyId);
                        // TODO (Phase 1.2): Process the ActivityPub JSON (Follow, Accept, Update, etc.)
                    }
                    else
                    {
                        _logger.LogWarning("Cryptographic verification FAILED for payload from {KeyId}. Dropping payload.", payload.Signature.KeyId);
                    }
                    
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

        /// <summary>
        /// Fetches the remote public key and verifies the RSA signature of the payload.
        /// </summary>
        private async Task<bool> VerifySignatureAsync(InboxPayload payload)
        {
            try
            {
                // 1. Fetch the remote actor's public key (In production, this should be cached)
                using var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Accept", "application/activity+json");
                
                var remoteKeyResponse = await client.GetAsync(payload.Signature.KeyId);
                if (!remoteKeyResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to retrieve public key from {KeyId}.", payload.Signature.KeyId);
                    return false;
                }

                // Note: For Phase 1, we assume the remote server returns the raw PEM block at this endpoint.
                // A true Fediverse implementation extracts this from the Actor JSON ["publicKey"]["publicKeyPem"].
                var publicKeyPem = await remoteKeyResponse.Content.ReadAsStringAsync();

                // 2. Reconstruct the comparison string using the signed headers
                // For simplicity in Phase 1, we are using the raw body as the comparison base.
                // A strict HTTP Signature implementation requires rebuilding the exact header string.
                byte[] dataToVerify = Encoding.UTF8.GetBytes(payload.RawJson);
                byte[] signatureBytes = Convert.FromBase64String(payload.Signature.Signature);

                // 3. Verify the mathematical signature
                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKeyPem);

                return rsa.VerifyData(dataToVerify, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signature validation for {KeyId}.", payload.Signature.KeyId);
                return false;
            }
        }
    }

    public class InboxPayload
    {
        public string RawJson { get; set; }
        public SignatureVerifier Signature { get; set; }
    }
}
