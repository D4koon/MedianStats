using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static MedianStats.NomadMemory;
using static MedianStats.WinApi;
using static MedianStats.Util;
using MedianStats.Properties;

namespace MedianStats
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public static MainWindow mainInstance;
		public static string ExeDir;

		public MainWindow()
		{
			InitializeComponent();

			mainInstance = this;
			ExeDir = System.Reflection.Assembly.GetExecutingAssembly().Location.Substring(0, System.Reflection.Assembly.GetExecutingAssembly().Location.LastIndexOf('\\'));

			notifierText.AppendText(Settings.Default.notifierText.Length > 0 ? Settings.Default.notifierText : notifierTextDefault);

			notifyEnabled.IsChecked = Settings.Default.notifyEnabled;
			notifySuperior.IsChecked = Settings.Default.notifySuperior;
			mousefix.IsChecked = Settings.Default.mousefix;
			nopickup.IsChecked = Settings.Default.nopickup;
			toggle.IsChecked = Settings.Default.toggleShowItems;

			InitVolumeSliders();


			notifierText.TextChanged += (unused1, unused2) =>
			{
				// Because this trigges even if only the formating of the text changes we have to check here if the text has really changed.
				if (Settings.Default.notifierText.Equals(notifierText.GetAllText().Text)) {
					return;
				}
				Settings.Default.notifierText = notifierText.GetAllText().Text;
				Settings.Default.Save();
				notifier.NeedUpdateList = true;
			};

			// This is necessary to get the rights to for OpenProcess.
			// WARNING: If the program is run from Visual Studio this is not needes since the Process already has that rights
			NomadMemory.EnableSE();

			Task.Run(() => Main());

			this.Show();
		}

		private void InitVolumeSliders()
		{
			// Remove dummy.
			volumneSliders.Children.Clear();

			// Add a slider for each sound
			for (int i = 0; i < Settings.Default.notifierSounds.Sounds.Count; i++) {
				var soundIt = Settings.Default.notifierSounds[i];

				var soundconfig = new SoundConfig() { Sound = soundIt, ID = i };
				volumneSliders.Children.Add(soundconfig);

				notifier.Sounds.Add(i, Settings.Default.notifierSounds[i]);
			}
		}

		public void NotifierLineAsError(int lineNumber)
		{
			if (!CheckAccess()) {
				Dispatcher.Invoke(() => notifierText.SetLineBackgroundColor(lineNumber, Brushes.Red));
			}
		}

		public void NotifierLinesClearError()
		{
			if (!CheckAccess()) {
				Dispatcher.Invoke(() => notifierText.SetPropertyForAllText(TextElement.BackgroundProperty, Brushes.White));
			}
		}

		/// <summary>Dummy-value. this will not be used anyway</summary>
		const int HK_FLAG_D2STATS = 0; /*BitOR($HK_FLAG_DEFAULT, $HK_FLAG_NOUNHOOK);*/

		const int g_iGUIOptionsHotkey = 6;

		const int g_iColorRed	= 0xFF0000;
		const int g_iColorBlue	= 0x0066CC;
		const int g_iColorGold	= 0x808000;
		const int g_iColorGreen	= 0x008000;
		const int g_iColorPink	= 0xFF00FF;

		static readonly string notifierTextDefault = ByteArrayToString(HexStringToByteArray("0x3120322033203420756E69717565202020202020202020202020202020232054696572656420756E69717565730D0A73616372656420756E6971756520202020202020202020202020202020232053616372656420756E69717565730D0A2252696E67247C416D756C6574247C4A6577656C2220756E69717565202320556E69717565206A6577656C72790D0A225175697665722220756E697175650D0A7365740D0A2242656C6C61646F6E6E61220D0A22536872696E65205C28313022202020202020202020202020202020202320536872696E65730D0A23225175697665722220726172650D0A232252696E67247C416D756C6574222072617265202020202020202020202320526172652072696E677320616E6420616D756C6574730D0A2373616372656420657468207375706572696F7220726172650D0A0D0A225369676E6574206F66204C6561726E696E67220D0A2247726561746572205369676E6574220D0A22456D626C656D220D0A2254726F706879220D0A224379636C65220D0A22456E6368616E74696E67220D0A2257696E6773220D0A2252756E6573746F6E657C457373656E63652422202320546567616E7A652072756E65730D0A2247726561742052756E6522202020202020202020232047726561742072756E65730D0A224F72625C7C2220202020202020202020202020202320554D4F730D0A224F696C206F6620436F6E6A75726174696F6E220D0A232252696E67206F66207468652046697665220D0A0D0A232048696465206974656D730D0A686964652031203220332034206C6F77206E6F726D616C207375706572696F72206D6167696320726172650D0A6869646520225E2852696E677C416D756C6574292422206D616769630D0A68696465202251756976657222206E6F726D616C206D616769630D0A6869646520225E28416D6574687973747C546F70617A7C53617070686972657C456D6572616C647C527562797C4469616D6F6E647C536B756C6C7C4F6E79787C426C6F6F6473746F6E657C54757271756F6973657C416D6265727C5261696E626F772053746F6E652924220D0A6869646520225E466C61776C657373220D0A73686F77202228477265617465727C537570657229204865616C696E6720506F74696F6E220D0A686964652022284865616C696E677C4D616E612920506F74696F6E220D0A6869646520225E4B657924220D0A6869646520225E28456C7C456C647C5469727C4E65667C4574687C4974687C54616C7C52616C7C4F72747C5468756C7C416D6E7C536F6C7C536861656C7C446F6C7C48656C7C496F7C4C756D7C4B6F7C46616C7C4C656D7C50756C7C556D7C4D616C7C4973747C47756C7C5665787C4F686D7C4C6F7C5375727C4265727C4A61687C4368616D7C5A6F64292052756E652422"));
		
		bool g_bHotkeysEnabled = false;
		int g_hTimerCopyName = 0;
		string g_sCopyName = "";

		public enum PrintColor
		{
			White, Red, Lime, Blue, Gold, Grey, Black, Unk, Orange, Yellow, Green, Purple
		}

		public bool g_bnotifierChanged = false;

		public IntPtr g_hD2Client, g_hD2Common, g_hD2Win, g_hD2Lang;
		public IntPtr g_ahD2Handle;

		uint g_iD2pid;
		uint g_iUpdateFailCounter;

		public IntPtr g_pD2sgpt, g_pD2InjectPrint, g_pD2InjectString, g_pD2InjectGetString;

		object[][] guiOptionList = new object[][] {
			new object[] {"copy", 0x002D, "hk", "Copy item text", "HotKey_CopyItem"}, 
			new object[] {"copy-name", 0, "cb", "Only copy item name"}, 
			new object[] {"filter", 0x0124, "hk", "Inject/eject DropFilter", "HotKey_DropFilter"},
			new object[] {"toggle", 0x0024, "hk", "Switch Show Items between hold/toggle mode", "HotKey_ToggleShowItems"},
			new object[] {"readstats", 0x0000, "hk", "Read stats without tabbing out of the game", "HotKey_ReadStats"},
		};

		#region Main

		ShowItems showItems = new ShowItems();
		MouseFix mouseFix = new MouseFix();
		NoPickup noPickup = new NoPickup();
		public Notifier notifier = new Notifier();
		public Stats stats = new Stats();

		public void Main()
		{
			//AutoItApi._HotKey_Disable(HK_FLAG_D2STATS);

			//var hTimerUpdateDelay = AutoItApi.TimerInit();

			bool bIsIngame = false;
			
			while (true) {
				
				try {
					UpdateHandle();
					ErrorMsg = "";
				} catch (Exception ex) {
					ErrorMsg = ex.Message;
				}
				

				if (IsIngame()) {
					if (!bIsIngame) {
						// Reset the notify-cache
						Notifier.ItemCache = null;

						//AutoItApi.GUICtrlSetState(g_idnotifyTest, GUI_ENABLE);
					}

					InjectFunctions();

					mouseFix.Do();

					showItems.Do();

					noPickup.Do(bIsIngame);

					if (Settings.Default.notifyEnabled) {
						notifier.NotifierMain();
						
						// The following implementation is not pretty but it should work good enough for now
						NotifierLinesClearError();
						foreach (var errorLineNumber in notifier.MatchErrorLines) {
							NotifierLineAsError(errorLineNumber);
						}
					}

					bIsIngame = true;
				} else {
					if (bIsIngame) {
						//AutoItApi.GUICtrlSetState(g_idnotifyTest, GUI_DISABLE);
					}

					bIsIngame = false;
					g_hTimerCopyName = 0;
				}

				//if (g_hTimerCopyName && AutoItApi.TimerDiff(g_hTimerCopyName) > 10000) {
				//	g_hTimerCopyName = 0;

				//	if (bIsIngame) { PrintString("Item name multi-copy expired."); }
				//}

				Thread.Sleep(200);
			}
		}

		string ErrorMsg
		{
			set {
				if (!CheckAccess()) {
					Dispatcher.Invoke(() => errorMsg.Content = value);
				}
			}
		}

		private void Button_Click_Read(object sender, RoutedEventArgs e)
		{
			stats.UpdateStatValues();
			CreateGUI();

			statsListBasic.Items.Clear();
			statsListDefens.Items.Clear();
			statsListOffens.Items.Clear();

			foreach (var guiGroup in guiGroups) {
				ListView listView;
				switch (guiGroup.StatGroup) {
					case StatGroup.Basic:
						listView = statsListBasic;
						break;
					case StatGroup.Defense:
						listView = statsListDefens;
						break;
					case StatGroup.Offense:
						listView = statsListOffens;
						break;
					default:
						throw new Exception("Button_Click_Read() - Unknown StatGroup \"" + guiGroup.StatGroup + "\"");
				}

				listView.Items.Add(new Label() { Content = "=== " + guiGroup.ShortDescription + " ===", Padding = new Thickness(0) });

				foreach (var statItem in guiGroup.guiItems) {
					string text = statItem.Update();
					if (statItem.Description.Length > 0) {
						text += " | " + statItem.Description;
					}


					listView.Items.Add(new Label() { Content = text, Padding = new Thickness(0), FontFamily = new FontFamily("Consolas") });
				}
			}
		}

		private void Mousefix_Changed(object sender, RoutedEventArgs e)
		{
			bool value = ((CheckBox)sender).IsChecked.Value;
			Settings.Default.mousefix = value;
			Settings.Default.Save();
		}

		public bool IsIngame()
		{
			if (g_iD2pid == 0) {
				return false;
			}
			return MemoryRead(g_hD2Client + 0x11BBFC, g_ahD2Handle) != 0;
		}

		public int GetIlvl()
		{
			var apOffsetsIlvl = new int[] { 0, 0x14, 0x2C };
			var iRet = MemoryPointerRead(g_hD2Client + 0x11BC38, g_ahD2Handle, apOffsetsIlvl);
			if (iRet /*not*/ == (IntPtr)0) {
				PrintString("Hover the cursor over an item first.", PrintColor.Red);
			}
			return (int)iRet;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Settings.Default.Save();
			_CloseHandle();
		}

		public void _CloseHandle()
		{
			if (g_ahD2Handle != IntPtr.Zero) {
				MemoryClose(g_ahD2Handle);
				g_ahD2Handle = IntPtr.Zero;
				g_iD2pid = 0;
			}
		}

		public void UpdateHandle() {
			var hWnd = AutoItApi.WinGetHandle("Diablo II");
			if (hWnd == (IntPtr)0) {
				throw new Exception("UpdateHandle: Couldn't find Diablo II window");
			}
			var iPID = AutoItApi.WinGetProcess(hWnd);

			//if (iPID == -1) { return _CloseHandle(); }
			if (iPID == g_iD2pid) {
				// Already initialized
				return;
			}

			_CloseHandle();
			g_iUpdateFailCounter += 1;
			g_ahD2Handle = OpenProcess((int)iPID);
			if (g_ahD2Handle == IntPtr.Zero) {
				// https://docs.microsoft.com/en-au/windows/win32/debug/system-error-codes
				var lastWin32Error = Marshal.GetLastWin32Error();
				throw new Exception($"UpdateHandle: Couldn't open Diablo II memory handle. No Admin rights? lastWin32Error: {lastWin32Error}");
			}

			if (!UpdateDllHandles()) {
				_CloseHandle();
				logger.Debug("UpdateHandle: Couldn't update dll handles.");
				//throw new Exception("UpdateHandle: Couldn't update dll handles.");
			}

			if (InjectFunctions() == false) {
				_CloseHandle();
				throw new Exception("UpdateHandle: Couldn't inject functions.");
			}

			g_iUpdateFailCounter = 0;
			g_iD2pid = iPID;
			g_pD2sgpt = (IntPtr)MemoryRead(g_hD2Common + 0x99E1C, g_ahD2Handle);
		}

		public bool UpdateDllHandles()
		{
			var pLoadLibraryW = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
			if (IntPtr.Zero == pLoadLibraryW) { throw new Exception("UpdateDllHandles: Couldn't retrieve LoadLibraryA address."); }

			//var pAllocAddress = _MemVirtualAllocEx(g_ahD2Handle[1], 0, 0x100, BitOR(MEM_COMMIT, MEM_RESERVE), PAGE_EXECUTE_READWRITE);
			var pAllocAddress = VirtualAllocEx(g_ahD2Handle, IntPtr.Zero, 0x100, AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ExecuteReadWrite);
			if (pAllocAddress == IntPtr.Zero) {
				// https://docs.microsoft.com/en-au/windows/win32/debug/system-error-codes
				var lastWin32Error = Marshal.GetLastWin32Error();
				throw new Exception("UpdateDllHandles: Failed to allocate memory.");
			}

			string[] g_asDLL = { "D2Client.dll", "D2Common.dll", "D2Win.dll", "D2Lang.dll" };

			var iDLLs = g_asDLL.Length;
			IntPtr[] hDLLHandle = new IntPtr[iDLLs];
			var bFailed = false;

			for (int i = 0; i < iDLLs; i++) {
				MemoryWrite(pAllocAddress, g_ahD2Handle, g_asDLL[i] + '\0');
				hDLLHandle[i] = (IntPtr)RemoteThread(pLoadLibraryW, pAllocAddress);
				if (hDLLHandle[i] == IntPtr.Zero) {
					bFailed = true;
				}
			}

			g_hD2Client = hDLLHandle[0];
			g_hD2Common = hDLLHandle[1];
			g_hD2Win = hDLLHandle[2];
			g_hD2Lang = hDLLHandle[3];

			var pD2Inject = g_hD2Client + 0xCDE00;
			g_pD2InjectPrint = pD2Inject + 0x0;
			g_pD2InjectGetString = pD2Inject + 0x10;
			g_pD2InjectString = pD2Inject + 0x20;

			g_pD2sgpt = (IntPtr)MemoryRead(g_hD2Common + 0x99E1C, g_ahD2Handle);

			//_MemVirtualFreeEx(g_ahD2Handle[1], pAllocAddress, 0x100, MEM_RELEASE);
			var tets = Marshal.GetLastWin32Error();
			var freeRet = VirtualFreeEx(g_ahD2Handle, pAllocAddress, 0, FreeType.Release);
			var tets2 = Marshal.GetLastWin32Error();
			if (freeRet == false) {
				throw new Exception("UpdateDllHandles: Failed to free memory.");
			}
			if (bFailed) {
				logger.Debug("UpdateDllHandles: Couldn't retrieve dll addresses.");
				//throw new Exception("UpdateDllHandles: Couldn't retrieve dll addresses.");
				return false;
			}

			return true;
		}

		/*#cs
		D2Client.dll+CDE00 - 53                    - push ebx
		D2Client.dll+CDE01 - 68 *                  - push D2Client.dll+CDE20
		D2Client.dll+CDE06 - 31 C0                 - xor eax,eax
		D2Client.dll+CDE08 - E8 *                  - call D2Client.dll+7D850
		D2Client.dll+CDE0D - C3                    - ret 

		D2Client.dll+CDE10 - 8B CB                 - mov ecx,ebx
		D2Client.dll+CDE12 - 31 C0                 - xor eax,eax
		D2Client.dll+CDE14 - BB *                  - mov ebx,D2Lang.dll+9450
		D2Client.dll+CDE19 - FF D3                 - call ebx
		D2Client.dll+CDE1B - C3                    - ret 
		#ce*/


		public bool InjectCode(IntPtr pWhere, string sCode)
		{
			MemoryWriteHexString(pWhere, g_ahD2Handle, sCode);

			var iConfirm = MemoryRead(pWhere, g_ahD2Handle);
			//throw new Exception("Den vergleich unterhalb nochmal genau anschauen was da abgeht");
			return SwapEndian((IntPtr)iConfirm) == sCode.Substring(2, 8);
		}

		public bool InjectFunctions()
		{
			var iPrintOffset = IntPtr.Subtract((g_hD2Client + 0x7D850), (g_hD2Client + 0xCDE0D).ToInt32());

			var sWrite = "0x5368" + SwapEndian(g_pD2InjectString) + "31C0E8" + SwapEndian(iPrintOffset) + "C3";
			var bPrint = InjectCode(g_pD2InjectPrint, sWrite);

			sWrite = "0x8BCB31C0BB" + SwapEndian(g_hD2Lang + 0x9450) + "FFD3C3";
			var bGetString = InjectCode(g_pD2InjectGetString, sWrite);

			return bPrint && bGetString;
		}

		#endregion

		#region Hotkeys


		//public void HotKey_CopyStatsToClipboard() {
		//	if (!/*not*/ IsIngame()) { return; }

		//	UpdateStatValues();
		//	var sOutput = "";

		//	for $i = 0 to g_iNumStats - 1
		//		var iVal = GetStatValue($i);

		//		if ($iVal) {
		//			sOutput &= StringFormat("%s = %s%s", $i, $iVal, "\r\n"/*@CRLF*/);
		//		}
		//	}


		//	ClipPut(sOutput);
		//	PrintString("Stats copied to clipboard.");
		//}

		//public void HotKey_CopyItemsToClipboard() {
		//	if (!/*not*/ IsIngame()) { return; }

		//	var iItemsTxt = _MemoryRead(g_hD2Common + 0x9FB94, g_ahD2Handle);
		//	var pItemsTxt = _MemoryRead(g_hD2Common + 0x9FB98, g_ahD2Handle);

		//	var pBaseAddr, $iNameID, $sName, $iMisc;
		//	var sOutput = "";

		//	for $iClass = 0 to $iItemsTxt - 1
		//		$pBaseAddr = $pItemsTxt + 0x1A8 * $iClass;

		//		$iMisc = _MemoryRead($pBaseAddr + 0x84, g_ahD2Handle, "dword");
		//		$iNameID = _MemoryRead($pBaseAddr + 0xF4, g_ahD2Handle, "word");

		//		$sName = RemoteThread($g_pD2InjectGetString, $iNameID);
		//		$sName = _MemoryRead($sName, g_ahD2Handle, "wchar[100]");
		//		$sName = StringReplace($sName, @LF, "|");

		//		sOutput &= StringFormat("[class:%04i] [misc:%s] <%s>%s", $iClass, $iMisc ? 0 : 1, $sName, "\r\n"/*@CRLF*/);
		//	}

		//	ClipPut(sOutput);
		//	PrintString("Items copied to clipboard.");
		//}

		//public void HotKey_CopyItem(bool TEST = false)
		//	if (TEST || /*or*/!/*not*/ IsIngame() || /*or*/GetIlvl() == 0) { return; }

		//	var hTimerRetry = AutoItApi.TimerInit();
		//	var sOutput = "";

		//	while (sOutput == "" && AutoItApi.TimerDiff($hTimerRetry) < 10)
		//		// sOutput = _MemoryRead($g_hD2Sigma + 0x96AF28, g_ahD2Handle, "wchar[8192]")
		//		sOutput = _MemoryRead(0x00191FA4, g_ahD2Handle, "wchar[2048]") ; Magic ?;
		//	}/*wend*/

		//	sOutput = StringRegExpReplace(sOutput, "ÿc.", "");
		//	var asLines = StringSplit(sOutput, @LF);

		//	if (_GUI_Option("copy-name")) {
		//		if (g_hTimerCopyName == 0 || /*or*/!/*not*/ (ClipGet() == g_sCopyName)) { g_sCopyName = ""; }
		//		g_hTimerCopyName = AutoItApi.TimerInit();

		//		g_sCopyName &= $asLines[$asLines[0]] & "\r\n"/*@CRLF*/;
		//		ClipPut(g_sCopyName);

		//		var avItems = StringRegExp(g_sCopyName, "\r\n"/*@CRLF*/, $STR_REGEXPARRAYGLOBALMATCH);
		//		PrintString(StringFormat("%s item name(s) copied.", UBound($avItems)));
		//		return;
		//	}

		//	sOutput = "";
		//	for $i = $asLines[0] to 1 step -1
		//		if ($asLines[$i] !=/*<>*/ "") { sOutput &= $asLines[$i] & "\r\n"/*@CRLF*/;
		//	}

		//	ClipPut(sOutput);
		//	PrintString("Item text copied.");
		//}

		//public void HotKey_DropFilter(bool TEST = false)
		//	if (TEST || /*or*/!/*not*/ IsIngame()) { return; }

		//	var hDropFilter = GetDropFilterHandle();

		//	if ($hDropFilter) {
		//		if (EjectDropFilter($hDropFilter)) {
		//			PrintString("Ejected DropFilter.", ePrint.Green);
		//			_Log("HotKey_DropFilter", "Ejected DropFilter.");
		//		 } else {
		//			_Debug("HotKey_DropFilter", "Failed to eject DropFilter.")
		//		}
		//	} else {
		//		if (InjectDropFilter()) {
		//			PrintString("Injected DropFilter.", ePrint.Green);
		//			_Log("HotKey_DropFilter", "Injected DropFilter.");
		//		} else {
		//			_Debug("HotKey_DropFilter", "Failed to inject DropFilter.");
		//		}
		//	}
		//}

		private void Nopickup_Changed(object sender, RoutedEventArgs e)
		{
			Settings.Default.nopickup = ((CheckBox)sender).IsChecked.Value;
			Settings.Default.Save();
		}

		private void Toggle_Changed(object sender, RoutedEventArgs e)
		{
			if (IsIngame() == false) { return; }

			Settings.Default.toggleShowItems = ((CheckBox)sender).IsChecked.Value;
			Settings.Default.Save();

			showItems.ToggleShowItems();
		}

		private void NotifyEnabled_Changed(object sender, RoutedEventArgs e)
		{
			Settings.Default.notifyEnabled = ((CheckBox)sender).IsChecked.Value;
			Settings.Default.Save();
		}

		private void NotifySuperior_Changed(object sender, RoutedEventArgs e)
		{
			Settings.Default.notifySuperior = ((CheckBox)sender).IsChecked.Value;
			Settings.Default.Save();
		}

		#endregion

		#region GUI helper functions

		//public void _GUI_NewOption($iLine, $sOption, $sText, $sFunc = "")
		//	var iY = _GUI_LineY($iLine)*2 - _GUI_LineY(0);

		//	var idControl;
		//	var sOptionType = _GUI_OptionType($sOption);

		//	switch $sOptionType
		//		case null
		//			_Log("_GUI_NewOption", "Invalid option '" & $sOption & "'")
		//			exit
		//		case "hk"
		//			Call($sFunc, true)
		//			if (@error == 0xDEAD && @extended == 0xBEEF) {
		//				_Log("_GUI_NewOption", StringFormat("No hotkey function '%s' for option '%s'", $sFunc, $sOption))
		//				exit
		//			}

		//			var iKeyCode = _GUI_Option($sOption);
		//			if ($iKeyCode) {
		//				_KeyLock($iKeyCode)
		//				_HotKey_Assign($iKeyCode, $sFunc, $HK_FLAG_D2STATS, "[CLASS:Diablo II]")
		//			}

		//			$idControl = _GUICtrlHKI_Create($iKeyCode, _GUI_GroupX(), $iY, 120, 25);
		//			GUICtrlCreateLabel($sText, _GUI_GroupX() + 124, $iY + 4)
		//		case "cb"
		//			$idControl = GUICtrlCreateCheckbox($sText, _GUI_GroupX(), $iY);
		//			AutoItApi.GUICtrlSetState(-1, _GUI_Option($sOption) ? $GUI_CHECKED : $GUI_UNCHECKED)
		//		case } else {/*else*/
		//			_Log("_GUI_NewOption", "Invalid option type '" & $sOptionType & "'");
		//			exit
		//	endswitch

		//	g_avGUIOption[0][0] += 1;
		//	var iIndex = g_avGUIOption[0][0];

		//	g_avGUIOption[$iIndex][0] = $sOption;
		//	g_avGUIOption[$iIndex][1] = $idControl;
		//	g_avGUIOption[$iIndex][2] = $sFunc;
		//}

		public int GUI_OptionID(string sOption)
		{
			for (int i = 0; i < guiOptionList.Length; i++) {
				if ((string)guiOptionList[i][0] == sOption) { return i; }
			}
			throw new Exception("_GUI_OptionID: Invalid option '" + sOption + "'");
			return -1;
		}

		public object _GUI_Option(string sOption, object vValue = null)
		{
			var iOption = GUI_OptionID(sOption);
			var vOld = guiOptionList[iOption][1];

			if (vValue != null && vValue != vOld) {
				guiOptionList[iOption][1] = vValue;
			}

			return vOld;
		}

		//#EndRegion

		//#Region GUI

		public void CreateGUI() {

			//	var sTitle = !/*not*/ @Compiled ? "Test" : StringFormat("D2Stats %s - [%s]", FileGetVersion(@AutoItExe, "FileVersion"), FileGetVersion(@AutoItExe, "Comments"));

			var g1 = new GuiGroup("Base stats", StatGroup.Basic);
			g1._GUI_NewItem(01, " Strength Base: {000} Bonus: {359}%/{900}");
			g1._GUI_NewItem(02, "Dexterity Base: {002} Bonus: {360}%/{901}");
			g1._GUI_NewItem(03, " Vitality Base: {003} Bonus: {362}%/{902}");
			g1._GUI_NewItem(04, "   Energy Base: {001} Bonus: {361}%/{903}");

			var g2 = new GuiGroup("Other stats", StatGroup.Basic);
			g2._GUI_NewItem(00, "{076}% Life", "Maximum Life");
			g2._GUI_NewItem(01, "{077}% Mana", "Maximum Mana");
			
			g2._GUI_NewItem(06, "{080}% M.Find", "Magic Find");
			g2._GUI_NewItem(07, "{079}% G.Find", "Gold Find");
			g2._GUI_NewItem(08, "{085}% Exp.Gain", "Experience gained");
			g2._GUI_NewItem(09, "{479} M.Skill", "Maximum Skill Level");
			g2._GUI_NewItem(10, "{185} Sig.Stat [185:400/400]", "Signets of Learning. Up to 400 can be used||Any sacred unique item x1-25 + Catalyst of Learning ? Signet of Learning x1-25 + Catalyst of Learning|Any set item x1-25 + Catalyst of Learning ? Signet of Learning x1-25 + Catalyst of Learning|Unique ring/amulet/jewel/quiver + Catalyst of Learning ? Signet of Learning + Catalyst of Learning");
			g2._GUI_NewItem(11, "Veteran tokens [219:1/1]", "On Nightmare and Hell difficulty, you can find veteran monsters near the end of|each Act. There are five types of veteran monsters, one for each Act||[Class Charm] + each of the 5 tokens ? returns [Class Charm] with added bonuses| +1 to [Your class] Skill Levels| +20% to Experience Gained");

			g2._GUI_NewItem(10, "{096}%/{067}% Faster Run/Walk", "Faster Run/Walk");

			g2._GUI_NewItem(00, "{278} SF", "Strength Factor");
			g2._GUI_NewItem(01, "{485} EF", "Energy Factor");
			g2._GUI_NewItem(02, "{904}% F.Cap", "Factor cap. 100% means you don't benefit from more str/ene factor");

			g2._GUI_NewItem(03, "{409}% Buff.Dur", "Buff/Debuff/Cold Skill Duration");
			g2._GUI_NewItem(04, "{27}% Mana.Reg", "Mana Regeneration");

			var g3 = new GuiGroup("Minions", StatGroup.Basic);
			g3._GUI_NewItem(01, "{444}% Life");
			g3._GUI_NewItem(02, "{470}% Damage");
			g3._GUI_NewItem(03, "{487}% Resist");
			g3._GUI_NewItem(04, "{500}% AR", "Attack Rating");

			var g4 = new GuiGroup("Life/Mana", StatGroup.Basic);
			g4._GUI_NewItem(07, "{060}%/{062}% Leech", "Life/Mana Stolen per Hit");
			g4._GUI_NewItem(08, "{086}/{138} *aeK", "Life/Mana after each Kill");
			g4._GUI_NewItem(09, "{208}/{209} *oS", "Life/Mana on Striking");
			g4._GUI_NewItem(10, "{210}/{295} *oA", "Life/Mana on Attack");

			var g5 = new GuiGroup("Other", StatGroup.Basic);
			g5._GUI_NewItem(06, "RIP [108:1/1]", "Slain Monsters Rest In Peace");

			var g6 = new GuiGroup("Resistance", StatGroup.Defense);
			g6._GUI_NewItem(01, "{039}%", "Fire", g_iColorRed);
			g6._GUI_NewItem(02, "{043}%", "Cold", g_iColorBlue);
			g6._GUI_NewItem(03, "{041}%", "Lightning", g_iColorGold);
			g6._GUI_NewItem(04, "{045}%", "Poison", g_iColorGreen);
			g6._GUI_NewItem(05, "{037}%", "Magic", g_iColorPink);
			g6._GUI_NewItem(06, "{036}%", "Physical");

			g6._GUI_NewItem(03, "{171}% TCD", "Total Character Defense");
			g6._GUI_NewItem(06, "{035} MDR", "Magic Damage Reduction");
			g6._GUI_NewItem(05, "{034} PDR", "Physical Damage Reduction");
			g6._GUI_NewItem(07, "{338}% Dodge", "Chance to avoid melee attacks while standing still");
			g6._GUI_NewItem(08, "{339}% Avoid", "Chance to avoid projectiles while standing still");
			g6._GUI_NewItem(09, "{340}% Evade", "Chance to avoid any attack while moving");

			g6._GUI_NewItem(05, "{109}% CLR", "Curse Length Reduction");
			g6._GUI_NewItem(06, "{110}% PLR", "Poison Length Reduction");

			var g7 = new GuiGroup("Item/Skill", StatGroup.Defense, "Speed from items and skills behave differently. Use SpeedCalc to find your breakpoints");
			g7._GUI_NewItem(08, "{099}%/{069}% FHR", "Faster Hit Recovery");
			g7._GUI_NewItem(09, "{102}%/{069}% FBR", "Faster Block Rate");

			var g8 = new GuiGroup("Slow", StatGroup.Defense);
			g8._GUI_NewItem(10, "{150}%/{376}% Tgt.", "Slows Target / Slows Melee Target");
			g8._GUI_NewItem(11, "{363}%/{493}% Att.", "Slows Attacker / Slows Ranged Attacker");

			var g9 = new GuiGroup("Abs/Flat", StatGroup.Defense, "Absorb / Flat absorb");
			g9._GUI_NewItem(01, "{142}%/{143}", "Fire", g_iColorRed);
			g9._GUI_NewItem(02, "{148}%/{149}", "Cold", g_iColorBlue);
			g9._GUI_NewItem(03, "{144}%/{145}", "Lightning", g_iColorGold);
			g9._GUI_NewItem(04, "{146}%/{147}", "Magic", g_iColorPink);

			var g10 = new GuiGroup("Item/Skill", StatGroup.Offense, "Speed from items and skills behave differently. Use SpeedCalc to find your breakpoints");
			g10._GUI_NewItem(07, "{093}%/{068}% IAS", "Increased Attack Speed");
			g10._GUI_NewItem(11, "{105}%/0% FCR", "Faster Cast Rate");

			var g11 = new GuiGroup("Offens", StatGroup.Offense);
			g11._GUI_NewItem(02, "{025}% EWD", "Enchanced Weapon Damage");
			g11._GUI_NewItem(04, "{119}% AR", "Attack Rating");
			g11._GUI_NewItem(11, "{136}% CB", "Crushing Blow. Chance to deal physical damage based on target's current health");
			g11._GUI_NewItem(12, "{141}% DS", "Deadly Strike. Chance to double physical damage of attack");
			g11._GUI_NewItem(13, "{164}% UA", "Uninterruptable Attack");
			
			g11._GUI_NewItem(07, "{489} TTAD", "Target Takes Additional Damage");

			var g12 = new GuiGroup("Damage/Pierce", StatGroup.Offense , "Spell damage / -Enemy resist");
			g12._GUI_NewItem(08, "{329}%/{333}%", "Fire", g_iColorRed);
			g12._GUI_NewItem(09, "{331}%/{335}%", "Cold", g_iColorBlue);
			g12._GUI_NewItem(10, "{330}%/{334}%", "Lightning", g_iColorGold);
			g12._GUI_NewItem(11, "{332}%/{336}%", "Poison", g_iColorGreen);
			g12._GUI_NewItem(12, "{431}% PSD", "Poison Skill Duration", g_iColorGreen);
			g12._GUI_NewItem(13, "{357}%/0%", "Physical/Magic", g_iColorPink);

			var g13 = new GuiGroup("Weapon Damage", StatGroup.Offense);
			g13._GUI_NewItem(01, "{048}-{049}", "Fire", g_iColorRed);
			g13._GUI_NewItem(02, "{054}-{055}", "Cold", g_iColorBlue);
			g13._GUI_NewItem(03, "{050}-{051}", "Lightning", g_iColorGold);
			g13._GUI_NewItem(04, "{057}-{058}/s", "Poison/sec", g_iColorGreen);
			g13._GUI_NewItem(05, "{052}-{053}", "Magic", g_iColorPink);
			g13._GUI_NewItem(06, "{021}-{022}", "One-hand physical damage. Estimated; may be inaccurate, especially when dual wielding");
			g13._GUI_NewItem(07, "{023}-{024}", "Two-hand/Ranged physical damage. Estimated; may be inaccurate, especially when dual wielding");

			guiGroups = new List<GuiGroup>();
			guiGroups.Add(g1);
			guiGroups.Add(g2);
			guiGroups.Add(g3);
			guiGroups.Add(g4);
			guiGroups.Add(g5);
			guiGroups.Add(g6);
			guiGroups.Add(g7);
			guiGroups.Add(g8);
			guiGroups.Add(g9);
			guiGroups.Add(g10);
			guiGroups.Add(g11);
			guiGroups.Add(g12);
			guiGroups.Add(g13);
		}

		public enum StatGroup
		{
			Basic,
			Defense,
			Offense,
		}

		List<GuiGroup> guiGroups = new List<GuiGroup>();

		public class GuiGroup
		{
			public string ShortDescription;
			public StatGroup StatGroup;
			public string FullDescription;

			public List<GuiItem> guiItems = new List<GuiItem>();

			public GuiGroup(string shortDescription, StatGroup statGroup /*= StatGroup.Basic*/, string fullDescription = "")
			{
				this.ShortDescription = shortDescription;
				this.StatGroup = statGroup;
				this.FullDescription = fullDescription;
			}

			public void _GUI_NewItem(int bla1, string sText, string description = "", int color = 0)
			{
				guiItems.Add(new GuiItem(bla1, sText, description, color));
			}
		}

		public class GuiItem
		{
			string sText;
			public string Description;
			int iColor;

			public GuiItem(int bla1, string sText, string description = "", int color = 0)
			{
				this.sText = sText;
				this.Description = description;
			}

			public string Update()
			{
				string rsText = sText;
				int iMatches, iStatValue;

				var asMatches = Regex.Match(sText, "(\\[(\\d+):(\\d+)\\/(\\d+)\\])");
				iMatches = asMatches.Groups.Count;

				if (iMatches != 1 && iMatches != 5) {
					throw new Exception("GuiItem: Invalid coloring pattern '" + sText + "'");
					//exit
				} else if (iMatches == 5) {
					rsText = sText.Replace(asMatches.Groups[1].Value, "");
					iColor = g_iColorRed;

					iStatValue = mainInstance.stats.GetStatValue(int.Parse(asMatches.Groups[2].Value));
					if (iStatValue >= int.Parse(asMatches.Groups[3].Value)) {
						iColor = g_iColorGreen;
					} else if (iStatValue >= int.Parse(asMatches.Groups[4].Value)) {
						iColor = g_iColorGold;
					}
				}

				var asMatches2 = Regex.Matches(sText, "{(\\d+)}");
				for (int j = 0; j < asMatches2.Count; j += 1) { /*for j = 0 to UBound(asMatches2) - 1 step 2*/

					string sValue = mainInstance.stats.GetStatValue(int.Parse(asMatches2[j].Groups[1].Value)).ToString();
					rsText = rsText.Replace(asMatches2[j].Groups[0].Value, sValue);
				}

				//sText = StringStripWS(sText, BitOR(STR_STRIPLEADING, STR_STRIPTRAILING, STR_STRIPSPACES));
				//GUICtrlSetData(idControl, sText);
				//if (iColor !=/*<>*/ 0) { GUICtrlSetColor(idControl, iColor); }

				return rsText;
			}
		}

		//#EndRegion

		//	var iOption = 0;

		//	for $i = 1 to g_iGUIOptionsGeneral
		//		_GUI_NewOption($i-1, g_avGUIOptionList[$iOption][0], g_avGUIOptionList[$iOption][3], g_avGUIOptionList[$iOption][4]);
		//		$iOption += 1;
		//	}

		//	GUICtrlCreateTabItem("Hotkeys")
		//	for $i = 1 to g_iGUIOptionsHotkey
		//		_GUI_NewOption($i-1, g_avGUIOptionList[$iOption][0], g_avGUIOptionList[$iOption][3], g_avGUIOptionList[$iOption][4]);
		//		$iOption += 1;
		//	}

		//	for $i = 0 to g_iNumSounds - 1
		//		var iLine = 1 + $i*2

		//		var id = GUICtrlCreateSlider(60, _GUI_LineY($iLine), 200, 25, BitOR($TBS_TOOLTIPS, $TBS_AUTOTICKS, $TBS_ENABLESELRANGE))
		//		GUICtrlSetLimit(-1, 10, 0)
		//		GUICtrlSetOnEvent(-1, "OnChange_VolumeSlider")
		//		_GUICtrlSlider_SetTicFreq($id, 1)

		//		_GUI_NewTextBasic($iLine, "Sound " & ($i + 1), false)

		//		GUICtrlCreateButton("Test", 260, _GUI_LineY($iLine), 60, 25)
		//		GUICtrlSetOnEvent(-1, "OnClick_VolumeTest")

		//		if ($i == 0) { g_idVolumeSlider = $id
		//		_GUI_Volume($i, 5)
		//	}
		//	LoadGUIVolume()

		//	GUICtrlCreateTabItem("About")
		//	_GUI_GroupX(8)
		//	_GUI_NewTextBasic(00, "Made by Wojen and Kyromyr, using Shaggi's offsets.", false)
		//	_GUI_NewTextBasic(01, "Layout help by krys.", false)
		//	_GUI_NewTextBasic(02, "Additional help by suchbalance and Quirinus.", false)
		//	_GUI_NewTextBasic(03, "Sounds by MurderManTX and Cromi38.", false)

		//	_GUI_NewTextBasic(05, "If you're unsure what any of the abbreviations mean, all of", false)
		//	_GUI_NewTextBasic(06, " them should have a tooltip when hovered over.", false)

		//	_GUI_NewTextBasic(08, "Hotkeys can be disabled by setting them to ESC.", false)

		//	GUICtrlCreateButton("Forum", 4 + 0*62, $inotifyY, 60, 25)
		//	GUICtrlSetOnEvent(-1, "OnClick_Forum")

		//	GUICtrlCreateTabItem("")
		//	UpdateGUI()
		//	GUIRegisterMsg($WM_COMMAND, "WM_COMMAND")

		//	GUISetState(@SW_SHOW)
		//}

		//public void UpdateGUIOptions() {
		//	var sType, $sOption, $idControl, $sFunc, $vValue, $vOld

		//	for $i = 1 to _GUI_OptionCount()
		//		_GUI_OptionByRef($i, $sOption, $idControl, $sFunc)

		//		$sType = _GUI_OptionType($sOption)
		//		$vOld = _GUI_Option($sOption)
		//		$vValue = $vOld

		//		switch $sType
		//			case "hk"
		//				$vValue = _GUICtrlHKI_GetHotKey($idControl)
		//			case "cb"
		//				$vValue = BitAND(GUICtrlRead($idControl), $GUI_CHECKED) ? 1 : 0
		//		endswitch

		//		if not ($vOld == $vValue) {
		//			_GUI_Option($sOption, $vValue)

		//			if ($sType == "hk") {
		//				if ($vOld) { _HotKey_Assign($vOld, 0, $HK_FLAG_D2STATS)
		//				if ($vValue) { _HotKey_Assign($vValue, $sFunc, $HK_FLAG_D2STATS, "[CLASS:Diablo II]")
		//			}
		//		}
		//	}/*next*/

		//	var bEnable = IsIngame()
		//	if ($bEnable !=/*<>*/ g_bHotkeysEnabled) {
		//		if ($bEnable) {
		//			_HotKey_Enable()
		//		} else {/*else*/
		//			AutoItApi._HotKey_Disable($HK_FLAG_D2STATS)
		//		}
		//		g_bHotkeysEnabled = $bEnable
		//	}
		//}

		#endregion

		#region Injection
		public uint RemoteThread(IntPtr pFunc)
		{
			// $var is in EBX register
			return RemoteThread(pFunc, IntPtr.Zero);
		}

		public uint RemoteThread(IntPtr pFunc, IntPtr iVar)
		{
			// $var is in EBX register

			//var aResult = DllCall(g_ahD2Handle[0], "ptr", "CreateRemoteThread", "ptr", g_ahD2Handle[1], "ptr", 0, "uint", 0, "ptr", pFunc, "ptr", iVar, "dword", 0, "ptr", 0);
			var aResult = CreateRemoteThread(g_ahD2Handle, (IntPtr)0, 0, pFunc, (IntPtr)iVar, 0, (IntPtr)0);
			var hThread = aResult;
			if (hThread == IntPtr.Zero) { throw new Exception("RemoteThread: Couldn't create remote thread."); }

			WaitForSingleObject(hThread);

			//var tDummy = AutoItApi.DllStructCreate("dword");
			uint lpExitCode;
			// NOTE: Eventuell muss ich aber die intere variante von GetExitCodeThread in d2 verwenden? https://docs.microsoft.com/en-us/archive/blogs/jonathanswift/dynamically-calling-an-unmanaged-dll-from-net-c
			//DllCall(g_ahD2Handle[0], "bool", "GetExitCodeThread", "handle", hThread, "ptr", AutoItApi.DllStructGetPtr(tDummy));
			var test = GetExitCodeThread(hThread, out lpExitCode);
			//var iRet = Dec(AutoItApi.Hex(AutoItApi.DllStructGetData(tDummy, 1)));
			var iRet = lpExitCode;

			CloseHandle(hThread);
			return iRet;
		}

		public static string SwapEndian(IntPtr pAddress)
		{
			var bytes = BitConverter.GetBytes(pAddress.ToInt32());
			Array.Reverse(bytes);
			int result = BitConverter.ToInt32(bytes, 0);

			return result.ToString("X4");
		}

		public bool PrintString(string sString, PrintColor iColor = PrintColor.White)
		{
			if (!IsIngame()) {
				return false;
			}

			if (!WriteWString(sString)) {
				throw new Exception("PrintString: Failed to write string.");
			}

			try {
				RemoteThread(g_pD2InjectPrint, (IntPtr)iColor);
			} catch {
				throw new Exception("PrintString: Failed to create remote thread.");
			}

			return true;
		}

		public bool WriteWString(string sString)
		{
			if (!IsIngame()) {
				throw new Exception("WriteWString: not ingame.");
			}

			try {
				MemoryWrite(g_pD2InjectString, g_ahD2Handle, sString + '\0');
			} catch {
				throw new Exception("WriteWString: Failed to write string.");
			}

			return true;
		}

		//public void WriteString($sString)
		//	if (!/*not*/ IsIngame()) { return _Log("WriteString", "!/*not*/ ingame."); }

		//	_MemoryWrite($g_pD2InjectString, g_ahD2Handle, $sString, StringFormat("char[%s]", StringLen($sString) + 1));
		//	if (@error) { return _Log("WriteString", "Failed to write string."); }

		//	return true;
		//}

		//public void GetDropFilterHandle() {
		//	if (!/*not*/ WriteString("DropFilter.dll")) { return _Debug("GetDropFilterHandle", "Failed to write string.");

		//	var pGetModuleHandleA = GetProcAddress(GetModuleHandle("kernel32.dll"), "GetModuleHandleA");
		//	if (!/*not*/ $pGetModuleHandleA) { return _Debug("GetDropFilterHandle", "Couldn't retrieve GetModuleHandleA address.");

		//	return RemoteThread($pGetModuleHandleA, $g_pD2InjectString);
		//}

		/*#cs
		D2Client.dll+5907E - 83 3E 04              - cmp dword ptr [esi],04 { 4 }
		D2Client.dll+59081 - 0F85
		-->
		D2Client.dll+5907E - E9 *           - jmp DropFilter.dll+15D0 { PATCH_DropFilter }
		#ce*/

		//public void InjectDropFilter() {
		//	var sPath = FileGetLongName("DropFilter.dll", $FN_RELATIVEPATH);
		//	if (!/*not*/ FileExists(sPath)) { return _Debug("InjectDropFilter", "Couldn't find DropFilter.dll. Make sure it's in the same folder as " & @ScriptName & "."); }
		//	if (!/*not*/ WriteString(sPath)) { return _Debug("InjectDropFilter", "Failed to write DropFilter.dll path."); }

		//	var pLoadLibraryA = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
		//	if (!/*not*/ pLoadLibraryA) { return _Debug("InjectDropFilter", "Couldn't retrieve LoadLibraryA address."); }

		//	var iRet = RemoteThread(pLoadLibraryA, $g_pD2InjectString);
		//	if (@error) { return _Debug("InjectDropFilter", "Failed to create remote thread."); }

		//	var bInjected = 233 !=/*<>*/ _MemoryRead(g_hD2Client + 0x5907E, g_ahD2Handle, "byte");

		//	// TODO: Check if this is still needed
		//	if (iRet && $bInjected) {
		//		var hDropFilter = _WinAPI_LoadLibrary("DropFilter.dll");
		//		if ($hDropFilter) {
		//			var pEntryAddress = GetProcAddress($hDropFilter, "_PATCH_DropFilter@0");
		//			if ($pEntryAddress) {
		//				var pJumpAddress = $pEntryAddress - 0x5 - (g_hD2Client + 0x5907E);
		//				_MemoryWrite(g_hD2Client + 0x5907E, g_ahD2Handle, "0xE9" & SwapEndian($pJumpAddress), "byte[5]");
		//			} else {/*else*/
		//				_Debug("InjectDropFilter", "Couldn't find DropFilter.dll entry point.");
		//				iRet = 0;
		//			}
		//			_WinAPI_FreeLibrary($hDropFilter);
		//		} else {/*else*/
		//			_Debug("InjectDropFilter", "Failed to load DropFilter.dll.");
		//			iRet = 0;
		//		}
		//	}

		//	return iRet;
		//}

		//public void EjectDropFilter($hDropFilter)
		//	var pFreeLibrary = GetProcAddress(GetModuleHandle("kernel32.dll"), "FreeLibrary");
		//	if (!/*not*/ $pFreeLibrary) { return _Debug("EjectDropFilter", "Couldn't retrieve FreeLibrary address."); }

		//	var iRet = RemoteThread($pFreeLibrary, $hDropFilter);
		//	if (@error) { return _Debug("EjectDropFilter", "Failed to create remote thread."); }

		//	if (iRet) { _MemoryWrite(g_hD2Client + 0x5907E, g_ahD2Handle, "0x833E040F85", "byte[5]"); }

		//	return iRet;
		//}

		#endregion
	}
}
