using MedianStats.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MedianStats.MainWindow;

namespace MedianStats
{
	public class ShowItems
	{
		public IntPtr g_hD2Client { get { return mainInstance.g_hD2Client; } }
		public IntPtr g_ahD2Handle { get { return mainInstance.g_ahD2Handle; } }

		bool bShowItems = false;

		public void Do()
		{
			if (IsShowItemsEnabled()) {
				if (NomadMemory.MemoryRead(g_hD2Client + 0xFADB4, g_ahD2Handle) == 0) {
					if (bShowItems) {
						mainInstance.PrintString("not showing items.", PrintColor.Blue);
					}
					bShowItems = false;
				} else {
					bShowItems = true;
				}
				// If hotkey is not set, activate non-toggle-mode
				if ((int)mainInstance._GUI_Option("toggle") == 0) { ToggleShowItems(); }
			} else {
				bShowItems = false;
			}
		}

		/*#cs
		D2Client.dll+3AECF - A3 *                  - mov [D2Client.dll+FADB4],eax { [00000000] }
		-->
		D2Client.dll+3AECF - 90                    - nop 
		D2Client.dll+3AED0 - 90                    - nop 
		D2Client.dll+3AED1 - 90                    - nop 
		D2Client.dll+3AED2 - 90                    - nop 
		D2Client.dll+3AED3 - 90                    - nop 


		D2Client.dll+3B224 - CC                    - int 3 
		D2Client.dll+3B225 - CC                    - int 3 
		D2Client.dll+3B226 - CC                    - int 3 
		D2Client.dll+3B227 - CC                    - int 3 
		D2Client.dll+3B228 - CC                    - int 3 
		D2Client.dll+3B229 - CC                    - int 3 
		D2Client.dll+3B22A - CC                    - int 3 
		D2Client.dll+3B22B - CC                    - int 3 
		D2Client.dll+3B22C - CC                    - int 3 
		D2Client.dll+3B22D - CC                    - int 3 
		D2Client.dll+3B22E - CC                    - int 3 
		D2Client.dll+3B22F - CC                    - int 3 
		-->
		D2Client.dll+3B224 - 83 35 * 01            - xor dword ptr [D2Client.dll+FADB4],01 { [00000000] }
		D2Client.dll+3B22B - E9 B6000000           - jmp D2Client.dll+3B2E6


		D2Client.dll+3B2E1 - 89 1D *               - mov [D2Client.dll+FADB4],ebx { [00000000] }
		-->
		D2Client.dll+3B2E1 - E9 3EFFFFFF           - jmp D2Client.dll+3B224
		D2Client.dll+3B2E6 - 90                    - nop 
		#ce*/

		public void ToggleShowItems()
		{
			var sWrite1 = "0x9090909090";
			var sWrite2 = "0x8335" + SwapEndian(g_hD2Client + 0xFADB4) + "01E9B6000000";
			var sWrite3 = "0xE93EFFFFFF90"; //Jump within same DLL shouldn't require offset fixing

			var bRestore = IsShowItemsEnabled();
			if (bRestore) {
				sWrite1 = "0xA3" + SwapEndian(g_hD2Client + 0xFADB4);
				sWrite2 = "0xCCCCCCCCCCCCCCCCCCCCCCCC";
				sWrite3 = "0x891D" + SwapEndian(g_hD2Client + 0xFADB4);
			}

			NomadMemory.MemoryWriteHexString(g_hD2Client + 0x3AECF, g_ahD2Handle, sWrite1);
			NomadMemory.MemoryWriteHexString(g_hD2Client + 0x3B224, g_ahD2Handle, sWrite2);
			NomadMemory.MemoryWriteHexString(g_hD2Client + 0x3B2E1, g_ahD2Handle, sWrite3);

			NomadMemory.MemoryWrite(g_hD2Client + 0xFADB4, g_ahD2Handle, new byte[] { 0, 0, 0, 0 });
			mainInstance.PrintString(bRestore ? "Hold to show items." : "Toggle to show items.", PrintColor.Blue);
		}

		/// <summary>
		/// Returns true if toggle-mode is switched on. (D2Stats-feature, not from Median itself)
		/// </summary>
		public bool IsShowItemsEnabled()
		{
			var testByte = NomadMemory.MemoryRead(g_hD2Client + 0x3AECF, g_ahD2Handle, new byte[1])[0];
			var enabled = testByte == 0x90;
			//Debug.WriteLine("testbyte: " + testByte + " enabled? "+ enabled);
			return enabled;
		}
	}
}
