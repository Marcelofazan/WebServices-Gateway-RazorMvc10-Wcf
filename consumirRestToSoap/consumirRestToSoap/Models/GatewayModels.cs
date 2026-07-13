using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace consumirRestToSoap.Models
{
    public class DecryptionRequest
    {
        [JsonPropertyName("encryptedBodyBase64")]
        public string EncryptedBodyBase64 { get; set; } = string.Empty;

        [JsonPropertyName("encryptedAesBase64")]
        public string EncryptedAesBase64 { get; set; } = string.Empty;
    }
}