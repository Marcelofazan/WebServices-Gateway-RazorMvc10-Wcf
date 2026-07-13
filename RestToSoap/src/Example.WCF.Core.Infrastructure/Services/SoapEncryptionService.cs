using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Example.WCF.Core.Infrastructure.Models;

namespace Example.WCF.Core.Infrastructure.Services;

public class SoapEncryptionService(CertificateService certificateService)
{
	private readonly string wsuNameSpace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
	private readonly string wsseNameSpace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
	private readonly string digestNameSpace = "http://www.w3.org/2000/09/xmldsig#";
	private readonly string encryptionNameSpace = "http://www.w3.org/2001/04/xmlenc#";
	private readonly string wsseEncNameSpace = "http://docs.oasis-open.org/wss/oasis-wss-wssecurity-secext-1.1.xsd";
	private readonly byte[] aesKey = RandomNumberGenerator.GetBytes(32);
	private readonly byte[] aesIv = RandomNumberGenerator.GetBytes(16);

	public string EncryptMessage(string soapMessage, SoapElementIds elementIds)
	{
		XmlDocument xmlDocument = new();
		xmlDocument.LoadXml(soapMessage);

		XmlNamespaceManager namespaceManager = new(xmlDocument.NameTable);
		namespaceManager.AddNamespace("soap", "http://www.w3.org/2003/05/soap-envelope");
		namespaceManager.AddNamespace("wsu", wsuNameSpace);
		namespaceManager.AddNamespace("wsse", wsseNameSpace);
		namespaceManager.AddNamespace("ds", digestNameSpace);
		namespaceManager.AddNamespace("e", encryptionNameSpace);
		namespaceManager.AddNamespace("wsseEnc", wsseEncNameSpace);

		if (xmlDocument.GetElementsByTagName("soap:Body")[0] is XmlElement bodyElement)
		{
			XmlElement secureXBody = bodyElement.GetElementsByTagName("Body")[0] as XmlElement ?? throw new Exception("Could not find sx Body element");
			string canonicalizedBodyContent = Encoding.UTF8.GetString(SoapSigningService.CanonicalizeXmlBytes(secureXBody));

			byte[] encryptedBody = EncryptDataWithAes(canonicalizedBodyContent);
			byte[] encryptedAesKey = EncryptAesKeyWithPublicKey(aesKey);

			if (encryptedBody.Length == 0 || encryptedAesKey.Length == 0)
			{
				throw new Exception("Encrypted parts not found.");
			}

			string encryptedBodyBase64 = Convert.ToBase64String(encryptedBody);
			string encryptedKeyBase64 = Convert.ToBase64String(encryptedAesKey);
			string aesKeyId = "EK-" + Guid.NewGuid().ToString("N").ToUpper();
			string encryptedDataId = "ED-" + Guid.NewGuid().ToString("N").ToUpper();

			XmlElement encryptedBodyElement = CreateEncryptedBodyElement(xmlDocument, encryptedDataId, aesKeyId, encryptedBodyBase64);
			XmlElement encryptedKeyElement = CreateEncryptedKeyElement(xmlDocument, encryptedDataId, aesKeyId, encryptedKeyBase64);

			bodyElement.RemoveAll();
			bodyElement.SetAttribute("Id", wsuNameSpace, elementIds.BodyId);
			bodyElement.AppendChild(encryptedBodyElement);

			XmlNode? securityElement = xmlDocument.SelectSingleNode("//wsse:Security", namespaceManager);
			securityElement?.PrependChild(encryptedKeyElement);
		}

		return xmlDocument.OuterXml;
	}

	private byte[] EncryptDataWithAes(string elementContent)
	{
		using Aes aes = Aes.Create();
		aes.Key = aesKey;
		aes.IV = aesIv;
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;

		using ICryptoTransform encryptor = aes.CreateEncryptor();
		byte[] plainBytes = Encoding.UTF8.GetBytes(elementContent);
		byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

		return [.. aesIv, .. encryptedBytes];
	}

	private byte[] EncryptAesKeyWithPublicKey(byte[] aesKey)
	{
		RSA? publicKey = certificateService.GetPublicKey() ?? throw new Exception("Public key could not be found.");
		return publicKey.Encrypt(aesKey, RSAEncryptionPadding.Pkcs1);
	}

	private XmlElement CreateEncryptedBodyElement(XmlDocument xmlDocument, string encryptedDataId, string aesKeyId, string encryptedBodyBase64)
	{
		XmlElement encryptedDataElement = xmlDocument.CreateElement("e", "EncryptedData", encryptionNameSpace);
		XmlElement encryptionMethodElement = xmlDocument.CreateElement("e", "EncryptionMethod", encryptionNameSpace);
		XmlElement cipherDatElement = xmlDocument.CreateElement("e", "CipherData", encryptionNameSpace);
		XmlElement cipherValueElement = xmlDocument.CreateElement("e", "CipherValue", encryptionNameSpace);
		
		// Create KeyInfo element
		XmlElement keyInfoElement = xmlDocument.CreateElement("ds", "KeyInfo", digestNameSpace);
		XmlElement securityTokenReferenceElement =  xmlDocument.CreateElement("wsse", "SecurityTokenReference", wsseNameSpace);
		XmlElement referenceElement = xmlDocument.CreateElement("wsse", "Reference", wsseNameSpace);
		
		securityTokenReferenceElement.SetAttribute("TokenType",wsseEncNameSpace,
			"http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#EncryptedKey");
		referenceElement.SetAttribute("URI", $"#{aesKeyId}");

		securityTokenReferenceElement.AppendChild(referenceElement);
		keyInfoElement.AppendChild(securityTokenReferenceElement);
		
		encryptedDataElement.SetAttribute("Type", $"{encryptionNameSpace}Content");
		encryptedDataElement.SetAttribute("Id", encryptedDataId);
		encryptedDataElement.SetAttribute("xmlns:e", encryptionNameSpace);
		encryptionMethodElement.SetAttribute("Algorithm", "http://www.w3.org/2001/04/xmlenc#aes256-cbc");

		cipherValueElement.InnerText = encryptedBodyBase64;

		cipherDatElement.AppendChild(cipherValueElement);
		encryptedDataElement.AppendChild(encryptionMethodElement);
		encryptedDataElement.AppendChild(keyInfoElement);
		encryptedDataElement.AppendChild(cipherDatElement);

		return encryptedDataElement;
	}

	private XmlElement CreateEncryptedKeyElement(XmlDocument xmlDocument, string encryptedDataId, string aesKeyId, string encryptedBodyBase64)
	{
		string subjectKeyIdentifier = certificateService.GetSubjectKeyIdentifier();

		XmlElement encryptedKeyElement = xmlDocument.CreateElement("e", "EncryptedKey", encryptionNameSpace);
		XmlElement encryptionMethodElement = xmlDocument.CreateElement("e", "EncryptionMethod", encryptionNameSpace);
		XmlElement keyInfoElement = xmlDocument.CreateElement("ds","KeyInfo",digestNameSpace);
		XmlElement securityTokenReferenceElement = xmlDocument.CreateElement("wsse", "SecurityTokenReference", wsseNameSpace);
		XmlElement keyIdentifierElement = xmlDocument.CreateElement("wsse", "KeyIdentifier", wsseNameSpace);
		XmlElement cipherDatElement = xmlDocument.CreateElement("e", "CipherData", encryptionNameSpace);
		XmlElement cipherValueElement = xmlDocument.CreateElement("e", "CipherValue", encryptionNameSpace);
		XmlElement referenceListElement = xmlDocument.CreateElement("e", "ReferenceList", encryptionNameSpace);
		XmlElement dataReferenceListElement = xmlDocument.CreateElement("e", "DataReference", encryptionNameSpace);

		keyIdentifierElement.InnerText = subjectKeyIdentifier;
		cipherValueElement.InnerText = encryptedBodyBase64;

		encryptedKeyElement.SetAttribute("Id", aesKeyId);
		encryptedKeyElement.SetAttribute("xmlns:e", encryptionNameSpace);
		encryptionMethodElement.SetAttribute("Algorithm", "http://www.w3.org/2001/04/xmlenc#rsa-1_5");
		keyIdentifierElement.SetAttribute("EncodingType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary");
		keyIdentifierElement.SetAttribute("ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509SubjectKeyIdentifier");
		dataReferenceListElement.SetAttribute("URI", $"#{encryptedDataId}");

		securityTokenReferenceElement.AppendChild(keyIdentifierElement);
		keyInfoElement.AppendChild(securityTokenReferenceElement);
		cipherDatElement.AppendChild(cipherValueElement);
		referenceListElement.AppendChild(dataReferenceListElement);

		encryptedKeyElement.AppendChild(encryptionMethodElement);
		encryptedKeyElement.AppendChild(keyInfoElement);
		encryptedKeyElement.AppendChild(cipherDatElement);
		encryptedKeyElement.AppendChild(referenceListElement);

		return encryptedKeyElement;
	}
}
