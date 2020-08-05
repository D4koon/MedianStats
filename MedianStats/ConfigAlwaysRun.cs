using MedianStats.Properties;
using System;
using static MedianStats.MainWindow;

namespace MedianStats
{
	public class ConfigAlwaysRun
	{
		private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public IntPtr g_hD2Client { get { return mainInstance.g_hD2Client; } }
		public IntPtr g_ahD2Handle { get { return mainInstance.g_ahD2Handle; } }

		// Address: D2Client.dll+11C3EC
		IntPtr address { get { return g_hD2Client + 0x11C3EC; } }

		public void Do()
		{
			if (Settings.Default.alwaysRun == true && IsEnabled() == false) {
				Toggle();
			}
		}

		public void Toggle()
		{
			if (IsEnabled()) {
				mainInstance.PrintString("Always run OFF.", PrintColor.Blue);
				NomadMemory.MemoryWrite(address, g_ahD2Handle, new byte[] { 0 });
			} else {
				mainInstance.PrintString("Always run ON.", PrintColor.Blue);
				NomadMemory.MemoryWrite(address, g_ahD2Handle, new byte[] { 1 });
			}
		}

		/// <summary>
		/// Returns true if toggle-mode is switched on. (D2Stats-feature, not from Median itself)
		/// </summary>
		public bool IsEnabled()
		{
			var testByte = NomadMemory.MemoryRead(address, g_ahD2Handle, new byte[1])[0];
			var enabled = testByte == 0x1;
			//logger.Debug("AlwaysRun-testbyte: " + testByte + " enabled? "+ enabled);
			return enabled;
		}
	}
}
