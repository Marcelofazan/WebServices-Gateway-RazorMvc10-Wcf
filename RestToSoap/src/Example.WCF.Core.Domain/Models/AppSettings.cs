namespace Example.WCF.Core.Domain.Models;

public class AppSettings
{
	public string? CertStorePath { get; set; }
	public string? CertStorePassword { get; set; }
	public string? CertThumbprint { get; set; }
	public string? SoapEndpoint { get; set; }
	public string? SoapAction { get; set; }
}
