using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MedianStats.MainWindow;

namespace MedianStats
{
	public class NoPickup
	{
		public IntPtr g_hD2Client { get { return mainInstance.g_hD2Client; } }
		public IntPtr g_ahD2Handle { get { return mainInstance.g_ahD2Handle; } }

		// NOTE: bIsIngame is more a changed-event
		public void Do(bool bIsIngame)
		{
			if (mainInstance.IsIngame() && (bool)mainInstance._GUI_Option("nopickup") != IsEnabled()) {
				Set((bool)mainInstance._GUI_Option("nopickup"));
			}
		}

		/// <summary>
		/// Returns true if is switched on.
		/// </summary>
		public bool Set(bool isEnabled)
		{
			byte value = (byte)(isEnabled ? 1 : 0);

			var test = NomadMemory._MemoryWrite(g_hD2Client + 0x11C2F0, g_ahD2Handle, new byte[] { value });

			if (test == 1) {
				mainInstance.PrintString("NoPickup: " + (isEnabled ? "true" : "false"), ePrint.Blue);
			} else {
				mainInstance.PrintString("Setting NoPickup to " + (isEnabled ? "true" : "false") + " failed.", ePrint.Red);
			}

			return test == 1;
		}

		/// <summary>
		/// Returns true if is switched on.
		/// </summary>
		public bool IsEnabled()
		{
			var testByte = NomadMemory._MemoryRead(g_hD2Client + 0x11C2F0, g_ahD2Handle, new byte[1])[0];
			var enabled = testByte == 1;
			//Debug.WriteLine("NoPickup::IsEnabled() " + enabled + " testByte: " + testByte);
			return enabled;
		}
	}
}
