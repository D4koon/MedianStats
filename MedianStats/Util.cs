using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedianStats
{
	public class Util
	{
		public static string ByteArrayToString(byte[] arr)
		{
			System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
			return enc.GetString(arr);
		}

		public static byte[] HexStringToByteArray(string hex)
		{
			// Remove "0x" at the start of the string.
			if (hex.StartsWith("0x")) {
				hex = hex.Substring(2);
			}

			return Enumerable.Range(0, hex.Length)
							 .Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
							 .ToArray();
		}
	}
}
