using Example.WCF.Core.Infrastructure.Clients;
using Example.WCF.Core.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Example.WCF.Core.Infrastructure;

public static class ServiceExtensions
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services)
	{
		services.AddTransient<SoapHeaderService>();
		services.AddTransient<SoapBodyService>();
		services.AddTransient<SoapSecurityService>();
		services.AddTransient<SoapMessageService>();
		services.AddTransient<CertificateService>();
		services.AddTransient<SoapSigningService>();
		services.AddTransient<SoapEncryptionService>();
		services.AddTransient<SoapDecryptionService>();
		services.AddTransient<SoapClient>();

		return services;
	}
}
