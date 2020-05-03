using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MedianStats.MainWindow;

namespace MedianStats
{
	/// <summary>
	/// NOTE: Fixed when "0x90909090909090909090"
	/// </summary>
	public class MouseFix
	{
		public IntPtr g_hD2Client { get { return mainInstance.g_hD2Client; } }
		public IntPtr g_ahD2Handle { get { return mainInstance.g_ahD2Handle; } }

		const string firstfixByte = "90";

		public void Do()
		{
			if ((bool)mainInstance._GUI_Option("mousefix") != IsMouseFixEnabled()) {
				Debug.WriteLine("option: " + (bool)mainInstance._GUI_Option("mousefix") + " enabled? " + IsMouseFixEnabled()  + " => mousefix changed");
				ToggleMouseFix();
			}
		}

		/*#cs
		D2Client.dll+42AE1 - A3 *                  - mov [D2Client.dll+11C3DC],eax { [00000000] }
		D2Client.dll+42AE6 - A3 *                  - mov [D2Client.dll+11C3E0],eax { [00000000] }
		->
		D2Client.dll+42AE1 - 90                    - nop 
		D2Client.dll+42AE2 - 90                    - nop 
		D2Client.dll+42AE3 - 90                    - nop 
		D2Client.dll+42AE4 - 90                    - nop 
		D2Client.dll+42AE5 - 90                    - nop 
		D2Client.dll+42AE6 - 90                    - nop 
		D2Client.dll+42AE7 - 90                    - nop 
		D2Client.dll+42AE8 - 90                    - nop 
		D2Client.dll+42AE9 - 90                    - nop 
		D2Client.dll+42AEA - 90                    - nop 
		#ce*/

		public void ToggleMouseFix()
		{
			var sWrite = IsMouseFixEnabled() ? "0xA3" + SwapEndian(g_hD2Client + 0x11C3DC) + "A3" + SwapEndian(g_hD2Client + 0x11C3E0) : "0x" + firstfixByte + "909090909090909090";
			var tetst = NomadMemory._MemoryWriteHexString(g_hD2Client + 0x42AE1, g_ahD2Handle, sWrite /*, "byte[10]"*/);
		}

		public bool IsMouseFixEnabled()
		{
			var testbyte = NomadMemory._MemoryRead(g_hD2Client + 0x42AE1, g_ahD2Handle, new byte[1])[0];
			return testbyte.ToString("X2") == firstfixByte;
		}
	}
}
