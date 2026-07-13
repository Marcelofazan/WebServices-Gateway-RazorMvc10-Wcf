
using System.Xml;
using Example.WCF.Core.Domain.Constants;
using Example.WCF.Core.Domain.Enums;
using Example.WCF.Core.Domain.Models;
using Example.WCF.Core.Infrastructure.Models;

namespace Example.WCF.Core.Infrastructure.Services;

public class SoapMessageService(
	SoapSecurityService soapSecurityService,
	SoapSigningService soapSigningService,
	SoapEncryptionService soapEncryptionService)
{
	private readonly SoapElementIds soapElementIds = new()
	{
		TimestampId = $"TS-" + Guid.NewGuid().ToString("N").ToUpper(),
		BinarySecurityTokenId = $"BST-" + Guid.NewGuid().ToString("N").ToUpper(),
		BodyId = $"BD-" + Guid.NewGuid().ToString("N").ToUpper()
	};
	public string GenerateSoapMessage(SoapRequest soapRequest)
	{
		XmlDocument xmlDocument = new();
		
		XmlNamespaceManager namespaceManager = new(xmlDocument.NameTable);
		namespaceManager.AddNamespace("soap", SoapNamespaces.SoapEnvelope);
		namespaceManager.AddNamespace("i", SoapNamespaces.MessageSchema);
		namespaceManager.AddNamespace("a", SecureXNamespaces.ProductContract[soapRequest.Header.ProductId]);

		XmlElement soapEnvelopeElement = xmlDocument.CreateElement("soap", "Envelope", SoapNamespaces.SoapEnvelope);
		XmlElement headerElement = SoapHeaderService.CreateHeader(xmlDocument, soapRequest.Header ?? new());
		XmlElement bodyElement = SoapBodyService.CreateBody(xmlDocument, soapRequest, soapElementIds);

		soapEnvelopeElement.SetAttribute("xmlns:a", SecureXNamespaces.ProductContract[soapRequest.Header!.ProductId]);

		soapEnvelopeElement.AppendChild(headerElement);
		soapEnvelopeElement.AppendChild(bodyElement);

		string soapMessageWithSecurityElement = soapSecurityService.AddSecurityElement(soapEnvelopeElement.OuterXml, soapElementIds);
		string signedSoapMessage = "<Error>No Signature created</Error>";
		string encryptedSoapMessage = "<Error>Could not encrypt</Error>";

		try
		{
			signedSoapMessage = soapSigningService.SignMessage(soapMessageWithSecurityElement, soapElementIds);
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}

		try
		{
			encryptedSoapMessage = soapEncryptionService.EncryptMessage(signedSoapMessage, soapElementIds);
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}

		return encryptedSoapMessage;
	}

}
