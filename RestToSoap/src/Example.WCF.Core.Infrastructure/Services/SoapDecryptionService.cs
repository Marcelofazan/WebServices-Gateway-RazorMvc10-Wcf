using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Example.WCF.Core.Infrastructure.Services;

public class SoapDecryptionService(CertificateService certificateService)
{
	private readonly string wsuNameSpace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
	private readonly string wsseNameSpace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
	private readonly string digestNameSpace = "http://www.w3.org/2000/09/xmldsig#";
	private readonly string encryptionNameSpace = "http://www.w3.org/2001/04/xmlenc#";
	private readonly string wsseEncNameSpace = "http://docs.oasis-open.org/wss/oasis-wss-wssecurity-secext-1.1.xsd";

	public string DecryptMessage(string encryptedSoapMessage)
	{
		XmlDocument xmlDocument = new();
		xmlDocument.LoadXml(encryptedSoapMessage);

		XmlNamespaceManager namespaceManager = new(xmlDocument.NameTable);
		namespaceManager.AddNamespace("soap", "http://www.w3.org/2003/05/soap-envelope");
		namespaceManager.AddNamespace("wsu", wsuNameSpace);
		namespaceManager.AddNamespace("wsse", wsseNameSpace);
		namespaceManager.AddNamespace("ds", digestNameSpace);
		namespaceManager.AddNamespace("xenc", encryptionNameSpace);
		namespaceManager.AddNamespace("wsseEnc", wsseEncNameSpace);

		XmlNode? encryptedKeyNode = xmlDocument.SelectSingleNode("//xenc:EncryptedKey/xenc:CipherData/xenc:CipherValue", namespaceManager) ?? throw new Exception("Encrypted key not found.");
		byte[] encryptedAesKey = Convert.FromBase64String(encryptedKeyNode.InnerText);
		byte[] aesKey = DecryptAesKeyWithPrivateKey(encryptedAesKey);

		XmlNode? encryptedBodyNode = xmlDocument.SelectSingleNode("//xenc:EncryptedData/xenc:CipherData/xenc:CipherValue", namespaceManager) ?? throw new Exception("Encrypted body not found.");
		byte[] encryptedBody = Convert.FromBase64String(encryptedBodyNode.InnerText);
		string decryptedBody = DecryptDataWithAes(encryptedBody, aesKey);

		if (xmlDocument.GetElementsByTagName("soap:Body")[0] is XmlElement bodyNode)
		{
			bodyNode.RemoveAll();
			bodyNode.InnerXml = decryptedBody;

			byte[] bodyBytes = SoapSigningService.CanonicalizeXmlBytes(bodyNode);

			SoapSigningService.GenerateDigestValue(bodyBytes);
		}

		return xmlDocument.OuterXml;
	}

	private byte[] DecryptAesKeyWithPrivateKey(byte[] encryptedAesKey)
	{
		RSA? privateKey = certificateService.GetPrivateKey() ?? throw new Exception("Private key could not be found.");
		return privateKey.Decrypt(encryptedAesKey, RSAEncryptionPadding.Pkcs1);
	}

	private static string DecryptDataWithAes(byte[] encryptedData, byte[] aesKey)
	{
		using Aes aes = Aes.Create();
		aes.Key = aesKey;

		// Assuming the IV is the first 16 bytes of the encrypted data
		aes.IV = [.. encryptedData.Take(16)];

		// The actual encrypted data is the remaining bytes
		byte[] actualEncryptedData = [.. encryptedData.Skip(16)];

		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7; // Use PKCS7 padding as it's the most common

		using ICryptoTransform decryptor = aes.CreateDecryptor();
		byte[] decryptedBytes = decryptor.TransformFinalBlock(actualEncryptedData, 0, actualEncryptedData.Length);
		return Encoding.UTF8.GetString(decryptedBytes);
	}

	public string DebugDecryptBase64String(string encryptedBase64, string aesKeyBase64)
	{
		byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
		byte[] aesKey = DecryptAesKeyWithPrivateKey(Convert.FromBase64String(aesKeyBase64));

		return DecryptDataWithAes(encryptedBytes, aesKey);
	}
}
