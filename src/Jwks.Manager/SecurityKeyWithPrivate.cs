using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;

namespace Jwks.Manager
{

    /// <summary>
    /// This points to a JSON file in the format: 
    /// {
    ///  "Modulus": "",
    ///  "Exponent": "",
    ///  "P": "",
    ///  "Q": "",
    ///  "DP": "",
    ///  "DQ": "",
    ///  "InverseQ": "",
    ///  "D": ""
    /// }
    /// </summary>
    public class SecurityKeyWithPrivate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Parameters { get; set; }
        public string KeyId { get; set; }
        public string Type { get; set; }
        public string Algorithm { get; set; }
        public DateTime CreationDate { get; set; }

        public void SetParameters(SecurityKey key, Algorithm alg)
        {
            if (alg.Kty() == JsonWebAlgorithmsKeyTypes.EllipticCurve)
            {
                var ecdsa = (ECDsaSecurityKey)key;
                Parameters = JsonConvert.SerializeObject(ecdsa.ECDsa.ExportParameters(includePrivateParameters: true));
            }
            else
            {
                Parameters = JsonConvert.SerializeObject(key);
            }
            Type = alg.Kty();
            KeyId = key.KeyId;
            Algorithm = alg;
            CreationDate = DateTime.Now;
        }

        public SecurityKey GetSecurityKey()
        {
            SecurityKey securityKey;
            if (Type == "EC")
            {
                var ecdsaParameters = JsonConvert.DeserializeObject<ECParameters>(Parameters);
                securityKey = new ECDsaSecurityKey(ECDsa.Create(ecdsaParameters))
                {
                    KeyId = KeyId
                };
            }
            else
            {
                securityKey = JsonConvert.DeserializeObject<JsonWebKey>(Parameters);
            }

            return securityKey;
        }
    }
}