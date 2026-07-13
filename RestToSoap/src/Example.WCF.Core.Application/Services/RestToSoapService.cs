using Example.WCF.Core.Infrastructure.Clients;
using Example.WCF.Core.Infrastructure.Services;
using Example.WCF.Core.Domain.Models;
using Example.WCF.Core.Domain.Services;

namespace Example.WCF.Core.Application.Services;

public class RestToSoapService(SoapMessageService soapMessageService, SoapClient soapClient, AppSettingsService appSettingsService)
{
	private readonly string? soapEndpoint = appSettingsService.GetAppSettings().SoapEndpoint ?? "https://uatsecurexapi.comcorp.co.za/ConsumerDecryptedService.svc";
	public async Task<string> ConvertAndSend(SoapRequest soapRequest)
	{
		string soapMessage = soapMessageService.GenerateSoapMessage(soapRequest);
		string responseContent = "<Envelope><Message>No Response Received.</Message></Envelope>";
		
		try
		{
			if (!string.IsNullOrEmpty(soapEndpoint))
			{
				responseContent = await soapClient.SendSoapRequest(soapMessage, soapEndpoint);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e.Message);
		}

		return responseContent;
	}
}
