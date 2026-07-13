using System.Xml;
using Example.WCF.Core.Domain.Constants;
using Example.WCF.Core.Domain.Models;

namespace Example.WCF.Core.Infrastructure.Services;

public class SoapHeaderService
{
	public static XmlElement CreateHeader(XmlDocument xmlDocument, SecureXHeader secureXHeader)
	{
		XmlElement headerElement = xmlDocument.CreateElement("soap", "Header", SoapNamespaces.SoapEnvelope);
		XmlElement secureXHeaderElement = CreateSecureXHeader(xmlDocument, secureXHeader);

		headerElement.AppendChild(secureXHeaderElement);

		return headerElement;
	}

	private static XmlElement CreateSecureXHeader(XmlDocument xmlDocument, SecureXHeader secureXHeader)
	{
		XmlElement secureXHeaderElement = xmlDocument.CreateElement("h", "Header", SecureXNamespaces.CommonContract);

		secureXHeaderElement.SetAttribute("xmlns:i", SoapNamespaces.MessageSchema);
		secureXHeaderElement.SetAttribute("xmlns:h", SecureXNamespaces.CommonContract);

		XmlElement additionalElement = CreateAdditionalElement(xmlDocument, secureXHeader.Additional ?? []);
		XmlElement consumerBusinessUnitElement = xmlDocument.CreateElement("h", "ConsumerBusinessUnit", SecureXNamespaces.CommonContract);
		XmlElement consumerReferenceElement = xmlDocument.CreateElement("h", "ConsumerReference", SecureXNamespaces.CommonContract);
		XmlElement exchangeReferenceElement = xmlDocument.CreateElement("h", "ExchangeReference", SecureXNamespaces.CommonContract);
		XmlElement initiatingIPElement = xmlDocument.CreateElement("h", "InitiatingIP", SecureXNamespaces.CommonContract);
		XmlElement productIdElement = xmlDocument.CreateElement("h", "ProductId", SecureXNamespaces.CommonContract);
		XmlElement providerBusinessUnitElement = xmlDocument.CreateElement("h", "ProviderBusinessUnit", SecureXNamespaces.CommonContract);
		XmlElement providerReferenceElement = xmlDocument.CreateElement("h", "ProviderReference", SecureXNamespaces.CommonContract);
		XmlElement transactionStatusElement = xmlDocument.CreateElement("h", "TransactionStatus", SecureXNamespaces.CommonContract);

		consumerBusinessUnitElement.InnerText = secureXHeader.ConsumerBusinessUnit.ToString() ?? "";
		consumerReferenceElement.InnerText = secureXHeader.ConsumerReference ?? "";
		exchangeReferenceElement.InnerText = secureXHeader.ExchangeReference.ToString() ?? "";
		initiatingIPElement.InnerText = secureXHeader.InitiatingIP ?? "";
		productIdElement.InnerText = secureXHeader.ProductId.ToString() ?? "";
		providerBusinessUnitElement.InnerText = secureXHeader.ProviderBusinessUnit.ToString() ?? "";
		providerReferenceElement.InnerText = secureXHeader.ProviderReference ?? "";
		transactionStatusElement.InnerText = secureXHeader.TransactionStatus.ToString() ?? "";

		providerReferenceElement.SetAttribute("nil", SoapNamespaces.MessageSchema, "true");

		secureXHeaderElement.AppendChild(additionalElement);
		secureXHeaderElement.AppendChild(consumerBusinessUnitElement);
		secureXHeaderElement.AppendChild(consumerReferenceElement);
		secureXHeaderElement.AppendChild(exchangeReferenceElement);
		secureXHeaderElement.AppendChild(initiatingIPElement);
		secureXHeaderElement.AppendChild(productIdElement);
		secureXHeaderElement.AppendChild(providerBusinessUnitElement);
		secureXHeaderElement.AppendChild(providerReferenceElement);
		secureXHeaderElement.AppendChild(transactionStatusElement);

		return secureXHeaderElement;
	}

	private static XmlElement CreateAdditionalElement(XmlDocument xmlDocument, List<KeyValuePair<string, string>> additionalItems)
	{
		XmlElement additionalElement = xmlDocument.CreateElement("h", "Additional", SecureXNamespaces.CommonContract);

		if (additionalItems.Count == 0)
		{
			additionalElement.SetAttribute("nil", SoapNamespaces.MessageSchema, "true");
		}

		foreach (KeyValuePair<string, string> keyValuePair in additionalItems)
		{
			XmlElement keyValuePairElement = xmlDocument.CreateElement("h", "KeyValuePair", SecureXNamespaces.CommonContract);
			XmlElement keyElement = xmlDocument.CreateElement("h", "Key", SecureXNamespaces.CommonContract);
			XmlElement valueElement = xmlDocument.CreateElement("h", "Value", SecureXNamespaces.CommonContract);

			keyElement.InnerText = keyValuePair.Key;
			valueElement.InnerText = keyValuePair.Value;

			keyValuePairElement.AppendChild(keyElement);
			keyValuePairElement.AppendChild(valueElement);
			additionalElement.AppendChild(keyValuePairElement);
		}

		return additionalElement;
	}
}
