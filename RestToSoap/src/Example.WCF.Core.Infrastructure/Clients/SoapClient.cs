using System.Net.Http.Headers;
using System.Text;
using Example.WCF.Core.Domain.Services;

namespace Example.WCF.Core.Infrastructure.Clients;

public class SoapClient(AppSettingsService appSettingsService)
{
	private readonly string? soapAction = appSettingsService.GetAppSettings().SoapAction ?? "http://SecureX.ConsumerSubmitService/V1/IConsumerDecryptedService/Submit" ;
	public async Task<string> SendSoapRequest(string soapRequest, string soapEndpoint)
	{
		using HttpClient client = new();
		HttpRequestMessage request = new(HttpMethod.Post, soapEndpoint);

		StringContent content = new (soapRequest, Encoding.UTF8, new MediaTypeHeaderValue("application/soap+xml")
		{
			CharSet = "utf-8"
		});
		content.Headers.ContentType!.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("action", $"\"{soapAction}\""));
		request.Content = content;

		HttpResponseMessage response = await client.SendAsync(request);
		string responseContent = await response.Content.ReadAsStringAsync();

		return responseContent;
	}
}
