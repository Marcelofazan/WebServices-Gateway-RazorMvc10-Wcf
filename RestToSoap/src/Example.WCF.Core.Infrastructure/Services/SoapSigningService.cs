using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using Example.WCF.Core.Infrastructure.Models;

namespace Example.WCF.Core.Infrastructure.Services;

public class SoapSigningService(CertificateService certificateService)
{
	private readonly string wsuNameSpace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
	private readonly string wsseNameSpace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
	private readonly string digestNameSpace = "http://www.w3.org/2000/09/xmldsig#";
	private readonly string canonicalizationMethod = "http://www.w3.org/2001/10/xml-exc-c14n#";

	public string SignMessage(string soapMessage, SoapElementIds elementIds)
	{
		XmlDocument xmlDocument = new();
		xmlDocument.LoadXml(soapMessage);

		XmlNamespaceManager namespaceManager = new(xmlDocument.NameTable);
		namespaceManager.AddNamespace("soap", "http://www.w3.org/2003/05/soap-envelope");
		namespaceManager.AddNamespace("wsu", wsuNameSpace);
		namespaceManager.AddNamespace("wsse", wsseNameSpace);
		namespaceManager.AddNamespace("ds", digestNameSpace);

		XmlElement signatureElement = xmlDocument.CreateElement("ds", "Signature", digestNameSpace);

		signatureElement.SetAttribute("Id", "SIG-" + Guid.NewGuid().ToString("N").ToUpper());

		XmlElement signInfoElement = CreateSignedInfoElement(xmlDocument);
		XmlElement keyInfoElement = CreateKeyInfoElement(xmlDocument, elementIds.BinarySecurityTokenId ?? "BST");

		if (xmlDocument.GetElementsByTagName("soap:Body")[0] is XmlElement bodyNode)
		{
			byte[] bodyBytes = CanonicalizeXmlBytes(bodyNode);
			string bodyNodeDigestValue = GenerateDigestValue(bodyBytes);
			XmlElement bodyReferenceElement = CreateReferenceElement(xmlDocument, bodyNodeDigestValue, elementIds.BodyId ?? "BD");

			bodyNode?.SetAttribute("Id", wsuNameSpace, elementIds.BodyId);

			signInfoElement.AppendChild(bodyReferenceElement);
		}
		
		if (xmlDocument.GetElementsByTagName("wsu:Timestamp")[0] is XmlElement timestampNode)
		{
			byte[] timestampBytes = CanonicalizeXmlBytes(timestampNode);
			string timestampDigestValue = GenerateDigestValue(timestampBytes);
			XmlElement timestampReferenceElement = CreateReferenceElement(xmlDocument, timestampDigestValue, elementIds.TimestampId ?? "TS");

			signInfoElement.AppendChild(timestampReferenceElement);
		}

		signatureElement.AppendChild(signInfoElement);

		string signatureValue = GenerateSignatureValue(signInfoElement);

		XmlElement signatureValueElement = xmlDocument.CreateElement("ds", "SignatureValue", digestNameSpace);
		signatureValueElement.InnerText = signatureValue;

		signatureElement.AppendChild(signatureValueElement);
		signatureElement.AppendChild(keyInfoElement);

		XmlNode? securityElement = xmlDocument.SelectSingleNode("//wsse:Security", namespaceManager);
		XmlNode? binarySecurityTokenElement = xmlDocument.SelectSingleNode("//wsse:BinarySecurityToken", namespaceManager);

		securityElement?.InsertAfter(signatureElement, binarySecurityTokenElement);

		return xmlDocument.OuterXml;
	}

	private XmlElement CreateKeyInfoElement(XmlDocument xmlDocument, string binarySecurityTokenRefId)
	{
		XmlElement keyInfoElement = xmlDocument.CreateElement("ds", "KeyInfo", digestNameSpace);
		XmlElement securityTokenReferenceElement = xmlDocument.CreateElement("wsse", "SecurityTokenReference", wsseNameSpace);
		XmlElement referenceElement = xmlDocument.CreateElement("wsse", "Reference", wsseNameSpace);

		keyInfoElement.SetAttribute("Id", "KI-" + Guid.NewGuid().ToString("N").ToUpper());
		securityTokenReferenceElement.SetAttribute("Id", wsuNameSpace, "STR-" + Guid.NewGuid().ToString("N").ToUpper());
		referenceElement.SetAttribute("URI", $"#{binarySecurityTokenRefId}");
		referenceElement.SetAttribute("ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");

		securityTokenReferenceElement.AppendChild(referenceElement);
		keyInfoElement.AppendChild(securityTokenReferenceElement);

		return keyInfoElement;
	}

	private XmlElement CreateSignedInfoElement(XmlDocument xmlDocument)
	{
		XmlElement signedInfoElement = xmlDocument.CreateElement("ds", "SignedInfo", digestNameSpace);
		XmlElement canonicalizationMethodElement = xmlDocument.CreateElement("ds", "CanonicalizationMethod", digestNameSpace);
		XmlElement signatureMethodElement = xmlDocument.CreateElement("ds", "SignatureMethod", digestNameSpace);

		canonicalizationMethodElement.SetAttribute("Algorithm", canonicalizationMethod);
		signatureMethodElement.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#rsa-sha1");

		signedInfoElement.AppendChild(canonicalizationMethodElement);
		signedInfoElement.AppendChild(signatureMethodElement);

		return signedInfoElement;
	}

	private XmlElement CreateReferenceElement(XmlDocument xmlDocument, string digestValue, string elementURI)
	{
		XmlElement referenceElement = xmlDocument.CreateElement("ds", "Reference", digestNameSpace);
		XmlElement transformsElement = xmlDocument.CreateElement("ds", "Transforms", digestNameSpace);
		XmlElement transformElement = xmlDocument.CreateElement("ds", "Transform", digestNameSpace);
		XmlElement digestMethodElement = xmlDocument.CreateElement("ds", "DigestMethod", digestNameSpace);
		XmlElement digestValueElement = xmlDocument.CreateElement("ds", "DigestValue", digestNameSpace);

		referenceElement.SetAttribute("URI", $"#{elementURI}");
		transformElement.SetAttribute("Algorithm", canonicalizationMethod);
		digestMethodElement.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#sha1");

		digestValueElement.InnerText = digestValue;

		transformsElement.AppendChild(transformElement);
		referenceElement.AppendChild(transformsElement);
		referenceElement.AppendChild(digestMethodElement);
		referenceElement.AppendChild(digestValueElement);

		return referenceElement;
	}

    private string GenerateSignatureValue(XmlElement signedInfoElement)
    {
        byte[] signedInfoBytes = CanonicalizeXmlBytes(signedInfoElement);

        // 1. Obtém o RSA do serviço
        RSA? rsa = certificateService.GetPrivateKey();

        // 2. DIAGNÓSTICO DEFENSIVO RADICAL: Se retornar nulo, vamos inspecionar o certificado por trás do serviço
        if (rsa == null)
        {
            // Usamos reflexão para extrair o campo privado '_certificate' de dentro do seu CertificateService
            var field = certificateService.GetType().GetField("_certificate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var certReal = field?.GetValue(certificateService) as System.Security.Cryptography.X509Certificates.X509Certificate2;

            if (certReal == null)
            {
                throw new Exception("Falha Crítica: O CertificateService sequer conseguiu carregar o arquivo do certificado (Objeto X509Certificate2 está nulo). Verifique o caminho ou a senha no appsettings.");
            }

            if (!certReal.HasPrivateKey)
            {
                throw new Exception($"Falha de Infraestrutura: O certificado com o assunto '{certReal.Subject}' e Thumbprint '{certReal.Thumbprint}' FOI ENCONTRADO, mas ele NÃO POSSUI uma chave privada atrelada a ele! Ele possui apenas a chave pública. Você precisa gerar um novo PFX contendo a chave privada.");
            }

            // Tenta uma última cartada extraindo a chave via extensão direta do .NET moderno
            rsa = System.Security.Cryptography.X509Certificates.RSACertificateExtensions.GetRSAPrivateKey(certReal);

            if (rsa == null)
            {
                throw new Exception("Falha de Permissão/Contêiner: O certificado tem chave privada (HasPrivateKey = true), mas o Windows/SO negou o acesso à chave privada (Access Denied) ou o algoritmo não é RSA compatível.");
            }
        }

        // 3. Executa a assinatura utilizando a referência limpa
        try
        {
            byte[] signature = rsa.SignData(signedInfoBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);
        }
        finally
        {
            // Como o rsa veio de um serviço duradouro (Singleton), descartamos apenas se ele foi criado de forma avulsa
            // No .NET moderno, se você der .Dispose() em um rsa obtido de um certificado persistente, você pode quebrar as próximas requisições.
            // Por isso removi o 'using' implícito da declaração e gerenciamos com segurança.
        }
    }

    public static string GenerateDigestValue(byte[] elementBytes)
	{
		byte[] digest = SHA1.HashData(elementBytes);
		string digestBase64 = Convert.ToBase64String(digest);

		return digestBase64;
	}

	public static byte[] CanonicalizeXmlBytes(XmlElement element)
	{
		if (element == null)
			throw new ArgumentNullException(nameof(element), "Element cannot be null");

		XmlDocument tempDoc = new();
		XmlElement importedElement = (XmlElement)tempDoc.ImportNode(element, true);
		tempDoc.AppendChild(importedElement);

		XmlDsigExcC14NTransform transform = new();
		transform.LoadInput(tempDoc);

		using MemoryStream ms = new();
		Stream outputStream = (Stream)transform.GetOutput();
		outputStream.CopyTo(ms);

		return ms.ToArray();
	}
}