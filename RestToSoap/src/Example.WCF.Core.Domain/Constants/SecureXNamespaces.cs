using System;

namespace Example.WCF.Core.Domain.Constants;

public class SecureXNamespaces
{
	public static readonly string CommonContract = "http://SecureX.Common/V1";
	public static readonly string ServiceContract = "http://SecureX.ConsumerSubmitService/V1";
	public static readonly Dictionary<int, string> ProductContract = new() {
		{ 1, "http://IDX.Contract/V1" },
		{ 2, "http://IVX.Contract/V1" },
		{ 4, "http://FICAX.POID.Contract/V1" }
	 };
}
