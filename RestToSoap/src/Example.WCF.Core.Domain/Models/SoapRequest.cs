namespace Example.WCF.Core.Domain.Models;

public class SoapRequest
{
	public required SecureXHeader Header { get; set; }
	public required object Body { get; set; }
}
