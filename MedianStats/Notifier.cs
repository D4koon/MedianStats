using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using static MedianStats.MainWindow;
using static MedianStats.NomadMemory;

namespace MedianStats
{
	public class Notifier
	{
		public IntPtr g_hD2Client { get { return mainInstance.g_hD2Client; } }
		public IntPtr g_ahD2Handle { get { return mainInstance.g_ahD2Handle; } }
		public IntPtr g_hD2Common { get { return mainInstance.g_hD2Common; } }

		public bool MatchListHasError { get; private set; }

		const int g_iNumSounds = 6; // Max 31;

		/// <summary>Name, Tier flag, Last line of name</summary>
		public object[,] NotifyCache = null;

		public List<NotifyMatch> notifyMatchList = new List<NotifyMatch>(); //Flags, Regex

		public bool NeedUpdateList = true;

		public int[] g_iQualityColor = new int[] { 0x0, (int)ePrint.White, (int)ePrint.White, (int)ePrint.White, (int)ePrint.Blue, (int)ePrint.Lime, (int)ePrint.Yellow, (int)ePrint.Gold, (int)ePrint.Orange, (int)ePrint.Green };

		public static string[,] g_asnotifyFlags;

		public Notifier()
		{
			g_asnotifyFlags = new string[(int)enotifyFlags.Last, 32] {
				{ "0", "1", "2", "3", "4", "sacred", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" },
				{ "low", "normal", "superior", "magic", "set", "rare", "unique", "craft", "honor", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" },
				{ "eth", "socket", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" },
				{ "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" },
				{ "clr_none", "white", "red", "lime", "blue", "gold", "grey", "black", "clr_unk", "orange", "yellow", "green", "purple", "show", "hide", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""},
				{ "sound_none", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" },
				{ "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" }
			};

			for (int i = 1; i <= g_iNumSounds; i++) {
				g_asnotifyFlags[(int)enotifyFlags.Sound, i] = "sound" + i;
			}
		}

		public void NotifierMain()
		{
			NotifierCache();
			UpdateMatchList();

			int[] aiOffsets = new int[] { 0, 0x2C, 0x1C, 0x0 };
			var pPaths = _MemoryPointerRead(g_hD2Client + 0x11BBFC, g_ahD2Handle, aiOffsets);

			aiOffsets[3] = 0x24;
			var iPaths = _MemoryPointerRead(g_hD2Client + 0x11BBFC, g_ahD2Handle, aiOffsets);

			if (pPaths == IntPtr.Zero || iPaths == IntPtr.Zero) {
				return;
			}

			var bnotifySuperior = (bool)mainInstance._GUI_Option("notify-superior");

			Debug.WriteLine("========= start =========");

			for (int i = 0; i <= (uint)iPaths - 1; i++) {

				var pPath = (IntPtr)_MemoryRead(pPaths + 4 * i, g_ahD2Handle);
				var pUnit = (IntPtr)_MemoryRead(pPath + 0x74, g_ahD2Handle);

				while ((uint)pUnit > 0) {
					var unitAny = ReadProcessMemoryStruct<UnitAny>(g_ahD2Handle, pUnit);
					pUnit = (IntPtr)unitAny.pUnit;

					//Debug.WriteLine("unitAny.iUnitType: " + unitAny.iUnitType);
					// iUnitType 4 = item
					if (unitAny.iUnitType == 4) {
						var tItemData = ReadProcessMemoryStruct<ItemData>(g_ahD2Handle, (IntPtr)unitAny.pUnitData);

						//; Using the ear level field to check if we've seen this item on the ground before
						//; Resets when the item is picked up || /*or*/we move too far away
						if (!/*not*/ mainInstance.g_bnotifierChanged && tItemData.iEarLevel != 0) { continue; }
						int iNewEarLevel = 1;

						var bIsNewItem = (0x2000 & tItemData.iFlags) != 0;
						var bIsSocketed = (0x800 & tItemData.iFlags) != 0;
						var bIsEthereal = (0x400000 & tItemData.iFlags) != 0;

						//var sName = (string)NotifyCache[unitAny.iClass, 0];
						int iTierFlag = (int)NotifyCache[unitAny.iClass, 1];
						var sText = (string)NotifyCache[unitAny.iClass, 2];

						bool bnotify = false;

						int iFlagsSound = 0;
						int iFlagsColour = 0;

						for (int j = 0; j < notifyMatchList.Count; j++) {

							if (Regex.IsMatch(sText, notifyMatchList[j].Match)) {
								int iFlagsTier = notifyMatchList[j].Tier;
								int iFlagsQuality = notifyMatchList[j].Quality;
								int iFlagsMisc = notifyMatchList[j].Misc;
								iFlagsColour = notifyMatchList[j].Colour;
								iFlagsSound = notifyMatchList[j].Sound;

								if (iFlagsTier != 0 && 0 ==/*not*/ /*BitAND*/(iFlagsTier & iTierFlag)) { continue; }
								if (iFlagsQuality != 0 && 0 ==/*not*/ /*BitAND*/(iFlagsQuality & /*BitRotate*/(1 << (int)tItemData.iQuality - 1))) { continue; }
								if (bIsSocketed == false && 0 != /*BitAND*/(iFlagsMisc & NotifierFlag("socket"))) { continue; }

								if (bIsEthereal) {
									sText += " (Eth)";
								} else if (0 != (iFlagsMisc & NotifierFlag("eth"))) {
									continue;
								}

								if (iFlagsColour == NotifierFlag("hide")) {
									iNewEarLevel = 2;
								} else if (iFlagsColour != NotifierFlag("show")) {
									bnotify = true;
								}

								break;
							}
						}

						// Write the the status of item (1 => already seen no pickup | 2 => already seen pickup
						_MemoryWrite((IntPtr)unitAny.pUnitData + 0x48, g_ahD2Handle, new byte[] { (byte)iNewEarLevel });

						if (bnotify) {
							int iColor;

							if (iFlagsColour != 0) {
								iColor = iFlagsColour - 1;
							} else if ((eQuality)tItemData.iQuality == eQuality.Normal && iTierFlag == NotifierFlag("0")) {
								iColor = (int)ePrint.Orange;
							} else {
								iColor = g_iQualityColor[tItemData.iQuality];
							}

							if (bnotifySuperior && (eQuality)tItemData.iQuality == eQuality.Superior) { sText = "Superior " + sText; }

							mainInstance.PrintString("- " + sText, (ePrint)iColor);

							if (iFlagsSound != NotifierFlag("sound_none")) {
								NotifierPlaySound(iFlagsSound);
							}
						}
					}
				}
			}

			mainInstance.g_bnotifierChanged = false;
		}

		public int NotifierFlag(string sFlag)
		{
			for (int i = 0; i <= (int)enotifyFlags.Last - 1; i++) {

				for (int j = 0; j <= /*UBound(g_asNotifyFlags, UBOUND_COLUMNS)*/ g_asnotifyFlags.GetLength(1) - 1; j++) {

					if (g_asnotifyFlags[i, j] == "") {
						break;
					} else if (g_asnotifyFlags[i, j] == sFlag) {
						return i > (int)enotifyFlags.NoMask ? j : /*BitRotate(1, j, "D")*/ 1 << j;
					}
				}
			}

			throw new Exception("Error with NotifierFlag");
		}

		public static void NotifierPlaySound(int iSound)
		{
			var iVolume = mainInstance._GUI_Volume(iSound) * 10;
			if (iVolume > 0) {
				SoundPlayer sound = new SoundPlayer("./resources/sound1.wav");
				sound.Play();
			}
		}

		/// <summary>
		/// Returns true if initialized
		/// </summary>
		public bool NotifierCache()
		{
			if (mainInstance.IsIngame() == false) {
				return false;
			}

			if (NotifyCache != null) {
				// Already initialized
				return true;
			}

			var iItemsTxt = _MemoryRead(g_hD2Common + 0x9FB94, g_ahD2Handle);
			var pItemsTxt = (IntPtr)_MemoryRead(g_hD2Common + 0x9FB98, g_ahD2Handle);

			IntPtr pBaseAddr;
			IntPtr iNameID;
			IntPtr iName;

			NotifyCache = new object[iItemsTxt, 3];

			for (int iClass = 0; iClass <= iItemsTxt - 1; iClass++) {
				pBaseAddr = pItemsTxt + 0x1A8 * iClass;

				iNameID = (IntPtr)_MemoryRead(pBaseAddr + 0xF4, g_ahD2Handle, "word");
				iName = (IntPtr)mainInstance.RemoteThread(mainInstance.g_pD2InjectGetString, iNameID);
				//sName = _MemoryRead(iName, g_ahD2Handle, "wchar[100]");
				var nameRaw = _MemoryRead(iName, g_ahD2Handle, new byte[200]);
				var sName = Encoding.Unicode.GetString(nameRaw);

				//var sNameTest = sName.Substring(0, sName.IndexOf('\0'));

				sName = sName.Replace("\n", "|");
				sName = Regex.Replace(sName, "ÿc.", "");
				var sTier = "0";

				if (_MemoryRead(pBaseAddr + 0x84, g_ahD2Handle) > 0) {
					// Weapon / Armor

					var match = Regex.Match(sName, "[1-4]|[(]Sacred[)]");
					if (match.Success) {
						sTier = match.Value.Equals("(Sacred)") ? "sacred" : match.Value;
					}
				}

				// I dont know why we should keep sName makes no sence to me o.O but i leave it for now...
				NotifyCache[iClass, 0] = sName;
				NotifyCache[iClass, 1] = NotifierFlag(sTier);
				//g_avnotifyCache[iClass, 2] = StringRegExpReplace(sName, ".+\|", "");
				//var test = Regex.Replace(sName, ".+\\|", "");
				NotifyCache[iClass, 2] = sName.Substring(0, sName.IndexOf('\0'));

				//if (@error) {
				//	_Debug("notifierCache", StringFormat("Invalid tier flag '%s'", sTier));
				//	exit;
				//}
			}

			return true;
		}

		public void UpdateMatchList()
		{
			if (!NeedUpdateList) {
				return;
			}
			NeedUpdateList = false;
			mainInstance.g_bnotifierChanged = true;

			var asLines = ((string)mainInstance._GUI_Option("notify-text")).Split('\n');
			var iLines = asLines.Length;

			notifyMatchList = new List<NotifyMatch>();

			MatchListHasError = false;
			for (int i = 0; i < iLines; i++) {
				try {
					NotifyMatch notifyMatch = NotifyMatch.FromString(asLines[i]);
					if (notifyMatch != null) {
						notifyMatchList.Add(notifyMatch);
					}
				} catch (Exception) {
					MatchListHasError = true;
					Debug.WriteLine($"Warning: NotifierUpdateList() - Could not parse line {i}");
				}
			}
		}

		public List<string> NotifierHelp(string sInput)
		{
			if (NotifierCache() == false) {
				return new List<string>() { "Error updating NotifierCache - Probably not ingame" };
			}

			var iItems = NotifyCache.GetLength(0);
			List<string> asMatches = new List<string>();

			NotifyMatch notifyCompile = NotifyMatch.FromString(sInput);

			if (notifyCompile != null) {
				var sMatch = notifyCompile.Match;
				var iFlagsTier = notifyCompile.Flags[enotifyFlags.Tier];

				string sName;
				int iTierFlag;

				for (int i = 0; i <= iItems - 1; i++) {

					sName = (string)NotifyCache[i, 0];
					iTierFlag = (int)NotifyCache[i, 1];

					if (Regex.IsMatch(sName, sMatch)) {
						if (iFlagsTier != 0 && 0 == /*not*/ /*BitAND*/(iFlagsTier & iTierFlag)) { continue; }

						asMatches.Add((string)NotifyCache[i, 2]);
					}
				}
			}
			return asMatches;
		}

		public enum enotifyFlags
		{
			Tier, Quality, Misc, NoMask, Colour, Sound, Match, Last
		}

		public class NotifyMatch
		{
			public int Tier { get { return Flags[enotifyFlags.Tier]; } }
			public int Quality { get { return Flags[enotifyFlags.Quality]; } }
			public int Misc { get { return Flags[enotifyFlags.Misc]; } }
			public int NoMask { get { return Flags[enotifyFlags.NoMask]; } }
			public int Colour { get { return Flags[enotifyFlags.Colour]; } }
			public int Sound { get { return Flags[enotifyFlags.Sound]; } }

			public Dictionary<enotifyFlags, int> Flags = new Dictionary<enotifyFlags, int>() {
				{ enotifyFlags.Tier, 0 },
				{ enotifyFlags.Quality, 0 },
				{ enotifyFlags.Misc, 0 },
				{ enotifyFlags.NoMask, 0 },
				{ enotifyFlags.Colour, 0 },
				{ enotifyFlags.Sound, 0 }
			};

			public string Match = "";

			string orgString;
			string cleanString;

			public static NotifyMatch FromString(string sLine)
			{
				var matchLine = new NotifyMatch();
				if (matchLine.Parse(sLine) == false) {
					return null;
				}

				return matchLine;
			}

			internal bool Parse(string sLine)
			{
				orgString = sLine;

				// Remove Comments
				sLine = Regex.Replace(sLine, "#.*", "");
				sLine = sLine.Trim();
				sLine = Regex.Replace(sLine, "[ ]{2,}", "");
				cleanString = sLine;

				return ParseClean(sLine);
			}

			internal bool ParseClean(string sLine)
			{
				string sArg = "";
				bool bQuoted = false;
				bool bHasFlags = false;

				//avRet = new string[(int)enotifyFlags.Last];

				for (int i = 0; i < sLine.Length; i++) {
					var sChar = sLine[i];

					if (sChar == '"') {
						if (bQuoted) {
							Match = sArg;
							sArg = "";
						}

						bQuoted = !bQuoted;
					} else if (sChar == ' ' && !bQuoted) {
						if (ParseFlag(sArg)) { bHasFlags = true; }
						sArg = "";
					} else {
						sArg += sChar;
					}
				}

				if (ParseFlag(sArg)) {
					bHasFlags = true;
				}

				if (Match.Length == 0) {
					if (bHasFlags == false) {
						return false;
					}
					Match = ".+";
				}

				return true;
			}

			public bool ParseFlag(string sFlag)
			{
				if (sFlag.Length == 0) { return false; }

				int iFlag = 0;
				enotifyFlags iGroup = 0;
				if (ParseFlagIndex(sFlag, ref iFlag, ref iGroup) == false) {
					MessageBox.Show(string.Format("Unknown notifier flag '%s' in line:%s%s", sFlag, "\r\n"/*@CRLF*/, orgString), "D2Stats");
					return false;
				}

				if (iGroup < enotifyFlags.NoMask) {
					// $iFlag = BitOR(BitRotate(1, $iFlag, "D"), $avRet[$iGroup])
					iFlag = /*BitOR*/(/*BitRotate*/(1 << iFlag) | Flags[iGroup]);
				}
				Flags[iGroup] = iFlag;
				//notifyCompile.SetValue((enotifyFlags)iGroup, iFlag.ToString());

				return iGroup != enotifyFlags.Colour;
			}

			public bool ParseFlagIndex(string sFlag, ref int iFlag, ref enotifyFlags iGroup)
			{
				for (int groupIndex = 0; groupIndex <= (int)enotifyFlags.Last - 1; groupIndex++) {

					for (int flagIndex = 0; flagIndex <= g_asnotifyFlags.GetLength(1) - 1; flagIndex++) {

						if (g_asnotifyFlags[groupIndex, flagIndex].Length == 0) {
							break;
						} else if (g_asnotifyFlags[groupIndex, flagIndex].Equals(sFlag)) {
							iGroup = (enotifyFlags)groupIndex;
							iFlag = flagIndex;
							return true;
						}
					}
				}

				throw new Exception("notifierFlagRef");
				//return SetError(1, 0, 0);
			}

			public override string ToString()
			{
				var retString = new StringBuilder();
				retString.Append("Match: \"" + Match + "\" ");
				foreach (var item in Flags) {
					retString.Append(item.Key.ToString() + ": " + item.Value + " ");
				}
				return retString.ToString();
			}
		}

		public enum eQuality
		{
			None, Low, Normal, Superior, Magic, Set, Rare, Unique, Craft, Honorific
		}


		// === Old logic that was ported to c# ===

		//public bool NotifierCompileLine(string sLine, ref string[] avRet)
		//{
		//	//	$sLine = StringStripWS(StringRegExpReplace($sLine, "#.*", ""), BitOR($STR_STRIPLEADING, $STR_STRIPTRAILING, $STR_STRIPSPACES))
		//	// Remove Comments
		//	sLine = Regex.Replace(sLine, "#.*", "");
		//	sLine = sLine.Trim();
		//	sLine = Regex.Replace(sLine, "[ ]{2,}", "");

		//	string sArg = "";
		//	bool bQuoted = false, bHasFlags = false;

		//	//redim avRet[0];
		//	//redim avRet[enotifyFlags.Last];
		//	avRet = new string[(int)enotifyFlags.Last];

		//	for (int i = 0; i < sLine.Length; i++) {
		//		//sChar = StringMid(sLine, i, 1);
		//		var sChar = sLine[i];

		//		if (sChar == '"') {
		//			if (bQuoted) {
		//				//notifyCompile.Match = sArg;
		//				avRet[(int)enotifyFlags.Match] = sArg;
		//				sArg = "";
		//			}

		//			bQuoted = !bQuoted;
		//		} else if (sChar == ' ' && !bQuoted) {
		//			if (NotifierCompileFlag(sArg, ref avRet, sLine)) { bHasFlags = true; }
		//			sArg = "";
		//		} else {
		//			sArg += sChar;
		//		}
		//	}

		//	if (NotifierCompileFlag(sArg, ref avRet, sLine)) { bHasFlags = true; }

		//	if (avRet[(int)enotifyFlags.Match] == null) {
		//		if (bHasFlags == false) {
		//			return false;
		//		}
		//		avRet[(int)enotifyFlags.Match] = ".+";
		//	}

		//	return true;
		//}

		//public bool NotifierCompileFlag(string sFlag, ref string[] avRet, string sLine)
		//{
		//	if (sFlag.Length == 0) { return false; }

		//	int iFlag = 0;
		//	int iGroup = 0;
		//	if (!/*not*/ NotifierFlagRef(sFlag, ref iFlag, ref iGroup)) {
		//		MessageBox.Show(string.Format("Unknown notifier flag '%s' in line:%s%s", sFlag, "\r\n"/*@CRLF*/, sLine), "D2Stats");
		//		return false;
		//	}

		//	if (iGroup < (int)enotifyFlags.NoMask) {
		//		// NOTE: Ich weiß nichht genau was die zeile unterhalb soll. Mal genau beobachten!!
		//		// $iFlag = BitOR(BitRotate(1, $iFlag, "D"), $avRet[$iGroup])
		//		int temp = avRet[iGroup] == null ? 0 : int.Parse(avRet[iGroup]);
		//		//temp = notifyCompile.GetValue((enotifyFlags)iGroup);
		//		iFlag = /*BitOR*/(/*BitRotate*/(1 << iFlag) | temp);
		//	}
		//	avRet[iGroup] = iFlag.ToString();
		//	//notifyCompile.SetValue((enotifyFlags)iGroup, iFlag.ToString());

		//	return iGroup !=/*<>*/ (int)enotifyFlags.Colour;
		//}

		/// <summary>
		/// Searches g_asnotifyFlags for the indexes.
		/// </summary>
		//public bool NotifierFlagRef(string sFlag, ref int iFlag, ref int iGroup)
		//{
		//	for (int groupIndex = 0; groupIndex <= (int)enotifyFlags.Last - 1; groupIndex++) {

		//		for (int flagIndex = 0; flagIndex <= g_asnotifyFlags.GetLength(1) - 1; flagIndex++) {

		//			if (g_asnotifyFlags[groupIndex, flagIndex].Length == 0) {
		//				break;
		//			} else if (g_asnotifyFlags[groupIndex, flagIndex].Equals(sFlag)) {
		//				iGroup = groupIndex;
		//				iFlag = flagIndex;
		//				return true;
		//			}
		//		}
		//	}

		//	throw new Exception("notifierFlagRef");
		//	//return SetError(1, 0, 0);
		//}
	}
}
