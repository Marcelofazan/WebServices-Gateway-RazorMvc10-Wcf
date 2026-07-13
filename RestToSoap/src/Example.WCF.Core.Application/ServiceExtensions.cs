using Example.WCF.Core.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Example.WCF.Core.Application;

public static class ServiceExtensions
{
	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		services.AddTransient<RestToSoapService>();
		return services;
	}
}
