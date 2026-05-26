using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.ActivityPub.Services
{
    public class SignatureVerifier
    {
        public string KeyId { get; private set; }
        public string Algorithm { get; private set; }
        public string[] Headers { get; private set; }
        public string Signature { get; private set; }

        public static SignatureVerifier Parse(string signatureHeader)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader))
            {
                throw new ArgumentException("Signature header is missing or empty.");
            }

            var parsed = new SignatureVerifier();
            var parts = signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var span = part.AsSpan().Trim();
                var equalsIndex = span.IndexOf('=');
                if (equalsIndex < 0) continue;

                var key = span.Slice(0, equalsIndex).ToString().ToLowerInvariant();
                var value = span.Slice(equalsIndex + 1).Trim('"').ToString();

                switch (key)
                {
                    case "keyid":
                        parsed.KeyId = value;
                        break;
                    case "algorithm":
                        parsed.Algorithm = value;
                        break;
                    case "headers":
                        parsed.Headers = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        break;
                    case "signature":
                        parsed.Signature = value;
                        break;
                }
            }

            if (string.IsNullOrEmpty(parsed.KeyId) || string.IsNullOrEmpty(parsed.Signature))
            {
                throw new ArgumentException("Signature header is malformed or missing required fields.");
            }

            // 'headers' defaults to 'date' if not explicitly defined in the HTTP Signature spec
            parsed.Headers ??= new[] { "date" };

            return parsed;
        }
    }
}
