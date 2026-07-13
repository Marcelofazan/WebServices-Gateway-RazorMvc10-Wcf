namespace Example.WCF.Core.Application.Models;

public class DecryptionRequest
{
	public required string EncryptedAesBase64 { get; set; }
	public required string EncryptedBodyBase64 { get; set; }
}
