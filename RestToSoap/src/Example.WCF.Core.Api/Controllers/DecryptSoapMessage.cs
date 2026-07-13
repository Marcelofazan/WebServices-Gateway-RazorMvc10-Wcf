using Example.WCF.Core.Application.Models;
using Example.WCF.Core.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Example.WCF.Core.Api.Controllers;

public class DecryptSoapMessage(SoapDecryptionService soapDecryptionService) : ControllerBase
{
	[HttpPost]
	[Route("/api/v1/decrypt-soap")]
	public ActionResult Post([FromBody] DecryptionRequest soapMessage)
	{
		try
		{
			string response = soapDecryptionService.DebugDecryptBase64String(soapMessage.EncryptedBodyBase64, soapMessage.EncryptedAesBase64);
			return new ContentResult
			{
				Content = response,
				ContentType = "application/soap+xml",
				StatusCode = 200
			};
		}
		catch (Exception e)
		{

			return BadRequest(e.Message);
		}
	}
}
