using Example.WCF.Core.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace Example.WCF.Core.Domain.Services;

public class AppSettingsService(IConfiguration configuration)
{
	public AppSettings GetAppSettings()
	{
		return new()
		{
			CertStorePath = configuration?["CertStorePath"],
			CertStorePassword = configuration?["CertStorePassword"],
		};
	}
}
