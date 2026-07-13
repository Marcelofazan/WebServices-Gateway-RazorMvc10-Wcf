using Example.WCF.Core.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Example.WCF.Core.Domain;

public static class ServiceExtensions
{
	public static IServiceCollection AddDomain(this IServiceCollection services)
	{
		services.AddTransient<AppSettingsService>();
		return services;
	}
}
