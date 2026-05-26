using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.ActivityPub.Database;

namespace Jellyfin.Plugin.ActivityPub.Services
{
    public class CryptographyService
    {
        private readonly ILogger<CryptographyService> _logger;

        public CryptographyService(ILogger<CryptographyService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Checks the isolated database for an existing keypair. If none exists, generates an RSA-2048 keypair.
        /// </summary>
        public void EnsureKeysExist()
        {
            using var db = new ActivityPubDbContext();
            
            // Automatically bootstrap the SQLite schema if it doesn't exist
            db.Database.EnsureCreated();

            if (!db.InstanceKeys.Any())
            {
                _logger.LogInformation("No ActivityPub cryptographic keys found. Generating new RSA-2048 keypair...");

                // Generate standard 2048-bit RSA keypair
                using var rsa = RSA.Create(2048);

                // Export to standard PEM formats (PKCS#8 for Private, SPKI for Public)
                var privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
                var publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();

                var keyInfo = new InstanceKeyInfo
                {
                    PrivateKeyPem = privateKeyPem,
                    PublicKeyPem = publicKeyPem,
                    CreatedAt = DateTime.UtcNow
                };

                db.InstanceKeys.Add(keyInfo);
                db.SaveChanges();

                _logger.LogInformation("ActivityPub RSA keypair successfully generated and saved to isolated database.");
            }
            else
            {
                _logger.LogInformation("ActivityPub cryptographic keys verified in database.");
            }
        }

        /// <summary>
        /// Retrieves the public key in PEM format for external ActivityPub actors to verify our signatures.
        /// </summary>
        public string GetPublicKeyPem()
        {
            using var db = new ActivityPubDbContext();
            var keyInfo = db.InstanceKeys.OrderByDescending(k => k.CreatedAt).FirstOrDefault();
            
            return keyInfo?.PublicKeyPem ?? throw new InvalidOperationException("ActivityPub keys have not been generated yet.");
        }

        /// <summary>
        /// Instantiates an RSA object using the stored private key, ready to sign outbound HTTP headers.
        /// </summary>
        public RSA GetPrivateKey()
        {
            using var db = new ActivityPubDbContext();
            var keyInfo = db.InstanceKeys.OrderByDescending(k => k.CreatedAt).FirstOrDefault();

            if (keyInfo == null)
            {
                throw new InvalidOperationException("ActivityPub keys have not been generated yet.");
            }

            var rsa = RSA.Create();
            rsa.ImportFromPem(keyInfo.PrivateKeyPem);
            return rsa;
        }
    }
}
