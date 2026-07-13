using System;

namespace Example.WCF.Core.Domain.Constants;

public class SoapNamespaces
{
	public static readonly string SoapEnvelope = "http://www.w3.org/2003/05/soap-envelope";
	public static readonly string WsseSecurity = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
	public static readonly string WsuSecurity = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
	public static readonly string MessageSchema = "http://www.w3.org/2001/XMLSchema-instance";
}
