namespace Example.WCF.Core.Domain.Models;

public class SecureXHeader
{
	public List<KeyValuePair<string, string>>? Additional { get; set; } = null; // For ArrayOfKeyValuePair
	public Guid? ConsumerBusinessUnit { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000000");// For ser:guid
	public string? ConsumerReference { get; set; } = "No Reference Provided"; // xs:string
	public long? ExchangeReference { get; set; } = 0; // xs:long
	public string? InitiatingIP { get; set; } = "127.0.0.0"; // xs:string
	public byte ProductId { get; set; } = 2; // xs:unsignedByte
	public Guid? ProviderBusinessUnit { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000000"); // ser:guid
	public string? ProviderReference { get; set; } = null; // xs:string
	public byte? TransactionStatus { get; set; } = 0;// xs:unsignedByte
}