using System.Xml;
using Example.WCF.Core.Infrastructure.Models;

namespace Example.WCF.Core.Infrastructure.Services;

public class SoapSecurityService(CertificateService certificateService)
{
	private readonly string wsuNameSpace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
	private readonly string wsseNameSpace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
	private SoapElementIds? _soapElementIds;
	public string AddSecurityElement(string soapMessage, SoapElementIds soapElementIds)
	{
		XmlDocument xmlDocument = new();
		xmlDocument.LoadXml(soapMessage);

		_soapElementIds = soapElementIds;

		XmlNamespaceManager namespaceManager = new(xmlDocument.NameTable);
		namespaceManager.AddNamespace("soap", "http://www.w3.org/2003/05/soap-envelope");
		namespaceManager.AddNamespace("wsu", wsuNameSpace);
		namespaceManager.AddNamespace("wsse", wsseNameSpace);

		XmlNode? envelopeElement = xmlDocument.SelectSingleNode("//soap:Envelope", namespaceManager);

		XmlAttribute wsseAttribute = xmlDocument.CreateAttribute("xmlns:wsse");
		XmlAttribute wsuAttribute = xmlDocument.CreateAttribute("xmlns:wsu");

		wsseAttribute.Value = wsseNameSpace;
		wsuAttribute.Value = wsuNameSpace;

		envelopeElement?.Attributes?.Append(wsseAttribute);
		envelopeElement?.Attributes?.Append(wsuAttribute);

		XmlNode? headerNode = xmlDocument.SelectSingleNode("//soap:Header", namespaceManager);

		XmlElement securityElement = xmlDocument.CreateElement("wsse", "Security", wsseNameSpace);

		CreateBinarySecurityToken(xmlDocument, securityElement);
		CreateTimestamp(xmlDocument, securityElement);

		headerNode?.PrependChild(securityElement);

		XmlElement? bodyElement = xmlDocument.GetElementsByTagName("soap:Body")[0] as XmlElement;

		bodyElement?.SetAttribute("xmlns:wsu", wsuNameSpace);
		bodyElement?.SetAttribute("Id", wsuNameSpace, soapElementIds.BodyId);

		return xmlDocument.InnerXml;
	}

	private void CreateBinarySecurityToken(XmlDocument xmlDocument, XmlElement securityElement)
	{
		XmlElement binarySecurityTokenElement = xmlDocument.CreateElement("wsse", "BinarySecurityToken", wsseNameSpace);
		binarySecurityTokenElement.SetAttribute("Id", wsuNameSpace, _soapElementIds?.BinarySecurityTokenId);
		binarySecurityTokenElement.SetAttribute("EncodingType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary");
		binarySecurityTokenElement.SetAttribute("ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");
		binarySecurityTokenElement.InnerText = certificateService.GetBinarySecurityToken();

		securityElement.AppendChild(binarySecurityTokenElement);
	}

	private void CreateTimestamp(XmlDocument xmlDocument, XmlElement securityElement)
	{
		string timestampCreated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
		string timeStampExpires = DateTime.UtcNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

		XmlElement timeStampElement = xmlDocument.CreateElement("wsu", "Timestamp", wsuNameSpace);
		XmlElement timeStampCreatedElement = xmlDocument.CreateElement("wsu", "Created", wsuNameSpace);
		XmlElement timeStampExpiresElement = xmlDocument.CreateElement("wsu", "Expires", wsuNameSpace);

		timeStampElement.SetAttribute("Id", wsuNameSpace, _soapElementIds?.TimestampId);

		timeStampCreatedElement.InnerText = timestampCreated;
		timeStampExpiresElement.InnerText = timeStampExpires;

		timeStampElement.AppendChild(timeStampCreatedElement);
		timeStampElement.AppendChild(timeStampExpiresElement);

		securityElement.AppendChild(timeStampElement);
	}
}
