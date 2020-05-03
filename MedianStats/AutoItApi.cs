using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MedianStats
{
	class AutoItApi
	{

		public static string Hex(uint uVal, int iDigits)
		{
			string szHexData = "0123456789ABCDEF";
			uint		k;
			uint	n = uVal;
			//string szBuffer = "";
			char[] szBuffer = new char[iDigits];

			for (int i=iDigits-1; i>=0; i--)
			{
				k = n % 16;
				szBuffer[i] = szHexData[(int)k];
				n = n / 16;
			}

			//szBuffer[iDigits] = '\0';

			if (n > 0) {
				throw new Exception();
				//return false;							// Left overs!
			} else {
				//return true;
			}

			return new string(szBuffer);
		}
		

		public static string StringLeft(string inputString, int length)
		{
			return inputString.Substring(0, length);
		}

		public static string Binary(IntPtr addr)
		{
			return Binary(addr.ToInt32());
		}

		public static string Binary(int addr)
		{
			var intToBinValue = Convert.ToString(addr, 2);
			return intToBinValue;
		}

		public static byte[] DllStructCreate(string sv_Type = "dword")
		{
			switch (sv_Type) {
				case "byte":
					return new byte[1];
				case "word":
					return new byte[2];
				case "dword":
					return new byte[4];
				default:
					throw new Exception("unknown size \"" + sv_Type + "\"");
			}
		}

		public static int DllStructGetData(byte[] byteArray, int element, string sv_Type)
		{
			if (element != 1) {
				throw new Exception();
			}

			switch (sv_Type) {
				case "byte":
					return byteArray[0];
				case "word":
					int z = BitConverter.ToInt16(byteArray, 0);
					return z;
				case "dword":
					int i = BitConverter.ToInt32(byteArray, 0);
					return i;
				default:
					throw new Exception("unknown size \"" + sv_Type + "\"");
			}
		}

		public static int DllStructGetSize(byte[] byteArray)
		{
			return byteArray.Length;
		}

		public static IntPtr DllStructGetPtr(byte[] byteArray)
		{
			int i = BitConverter.ToInt32(byteArray, 0);
			return (IntPtr)i;
		}

		public static void DllStructSetData(byte[] byteArray, int element, byte[] byteArray2)
		{
			if (element != 1) {
				throw new Exception();
			}

			byteArray = byteArray2;
		}

		internal static object TimerInit()
		{
			throw new NotImplementedException();
		}

		internal static int TimerDiff(object hTimerUpdateDelay)
		{
			throw new NotImplementedException();
		}

		internal static void _HotKey_Disable(object hK_FLAG_D2STATS)
		{
			throw new NotImplementedException();
		}

		internal static void GUICtrlSetState(object g_idnotifyTest, object gUI_ENABLE)
		{
			throw new NotImplementedException();
		}

		// Find window by Caption, and wait 1/2 a second and then try again.
		internal static IntPtr WinGetHandle(string className)
		{
			IntPtr hWnd = FindWindow(className, null);
			
			return hWnd;
		}

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern IntPtr FindWindow(string className, string windowName);


		internal static uint WinGetProcess(IntPtr hWnd)
		{
			uint processId = 0;
			GetWindowThreadProcessId(hWnd, out processId);

			return processId;
		}

		[DllImport("user32.dll", SetLastError = true)]
		static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
	}
}
