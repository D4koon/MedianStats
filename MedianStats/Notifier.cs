using MedianStats.IO;
using MedianStats.Properties;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static MedianStats.MainWindow;
using static MedianStats.NomadMemory;

namespace MedianStats
{
	public class Notifier
	{
		private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public static IntPtr g_hD2Client { get { return mainInstance.g_hD2Client; } }
		public static IntPtr g_ahD2Handle { get { return mainInstance.g_ahD2Handle; } }
		public static IntPtr g_hD2Common { get { return mainInstance.g_hD2Common; } }

		//public bool MatchListHasError { get; private set; }
		public ConcurrentBag<int> MatchErrorLines { get; private set; }

		/// <summary>Name, Tier flag, Last line of name</summary>
		public static List<CacheItem> ItemCache = null;

		public bool NeedUpdateList = true;

		PrintColor[] qualityColor = new PrintColor[] { 0, PrintColor.White, PrintColor.White, PrintColor.White, PrintColor.Blue, PrintColor.Lime, PrintColor.Yellow, PrintColor.Gold, PrintColor.Orange, PrintColor.Green };

		List<NotifyMatch> notifyMatchList = new List<NotifyMatch>(); //Flags, Regex

		public Notifier()
		{
		}

		public void Do()
		{
			InitItemCache();
			UpdateMatchList();

			int[] aiOffsets = new int[] { 0, 0x2C, 0x1C, 0x0 };
			var pPaths = MemoryPointerRead(g_hD2Client + 0x11BBFC, g_ahD2Handle, aiOffsets);

			aiOffsets[3] = 0x24;
			var iPaths = MemoryPointerRead(g_hD2Client + 0x11BBFC, g_ahD2Handle, aiOffsets);

			if (pPaths == IntPtr.Zero || iPaths == IntPtr.Zero) {
				return;
			}

			//logger.Debug("========= start =========");

			for (int i = 0; i < (uint)iPaths; i++) {

				var pPath = (IntPtr)MemoryRead(pPaths + 4 * i, g_ahD2Handle);
				var pUnit = (IntPtr)MemoryRead(pPath + 0x74, g_ahD2Handle);

				while ((uint)pUnit > 0) {
					var unitAny = ReadProcessMemoryStruct<UnitAny>(g_ahD2Handle, pUnit);
					
					pUnit = (IntPtr)unitAny.pUnitNext;

					//logger.Debug("unitAny.iUnitType: " + unitAny.iUnitType);
					
					if (unitAny.IsItem) {
						var tItemData = ReadProcessMemoryStruct<ItemData>(g_ahD2Handle, (IntPtr)unitAny.pUnitData);

						// Using the ear level field to check if we've seen this item on the ground before
						// Resets when the item is picked up or we move too far away
						if (!mainInstance.g_bnotifierChanged && tItemData.iEarLevel != 0) { continue; }
						int iNewEarLevel = 1;

						var isNewItem = (0x2000 & tItemData.iFlags) != 0;
						var isSocketed = (0x800 & tItemData.iFlags) != 0;
						var isEthereal = (0x400000 & tItemData.iFlags) != 0;

						var itemText = unitAny.Name;

						bool notify = false;

						int iFlagsSound = 0;
						int iFlagsColour = 0;

						for (int j = 0; j < notifyMatchList.Count; j++) {

							if (Regex.IsMatch(unitAny.FullText, notifyMatchList[j].Match)) {
								int iFlagsTier = notifyMatchList[j].Tier;
								int iFlagsQuality = notifyMatchList[j].Quality;
								int iFlagsMisc = notifyMatchList[j].Misc;
								iFlagsColour = notifyMatchList[j].Colour;
								iFlagsSound = notifyMatchList[j].Sound;

								if (iFlagsTier != 0 && 0 == (iFlagsTier & unitAny.TierFlag)) { continue; }
								if (iFlagsQuality != 0 && 0 == (iFlagsQuality & (1 << (int)tItemData.iQuality - 1))) { continue; }
								if (isSocketed == false && 0 != (iFlagsMisc & ItemFlags.GetFlagId("socket"))) { continue; }

								if (isEthereal) {
									itemText += " (Eth)";
								} else if (0 != (iFlagsMisc & ItemFlags.GetFlagId("eth"))) {
									continue;
								}

								if (iFlagsColour == ItemFlags.GetFlagId("hide")) {
									iNewEarLevel = 2;
								} else if (iFlagsColour != ItemFlags.GetFlagId("show")) {
									notify = true;
								}

								break;
							}
						}

						// Write the the status of item (1 => already seen no pickup | 2 => already seen pickup
						MemoryWrite((IntPtr)unitAny.pUnitData + 0x48, g_ahD2Handle, new byte[] { (byte)iNewEarLevel });

						if (notify) {
							PrintColor iColor;

							if (iFlagsColour != 0) {
								iColor = (PrintColor)iFlagsColour - 1;
							} else if ((ItemQuality)tItemData.iQuality == ItemQuality.Normal && unitAny.TierFlag == ItemFlags.GetFlagId("0")) {
								iColor = PrintColor.Orange;
							} else {
								iColor = qualityColor[tItemData.iQuality];
							}

							if (Settings.Default.notifySuperior && (ItemQuality)tItemData.iQuality == ItemQuality.Superior) { itemText = "Superior " + itemText; }

							mainInstance.PrintString("- " + itemText, iColor);

							if (iFlagsSound != -1) {
								MainWindow.mainInstance.Sounds.List[iFlagsSound].Play();
							}
						}
					}
				}
			}

			mainInstance.g_bnotifierChanged = false;
		}

		/// <summary>
		/// Returns true if initialized
		/// </summary>
		public static bool InitItemCache()
		{
			if (mainInstance.IsIngame() == false) {
				return false;
			}

			if (ItemCache != null) {
				// Already initialized
				return true;
			}

			var itemCount = MemoryRead(g_hD2Common + 0x9FB94, g_ahD2Handle);
			ItemCache = new List<CacheItem>(itemCount);

			var pItemsTxt = (IntPtr)MemoryRead(g_hD2Common + 0x9FB98, g_ahD2Handle);

			for (int iClass = 0; iClass < itemCount; iClass++) {
				IntPtr pBaseAddr = pItemsTxt + 0x1A8 * iClass;

				IntPtr iNameID = (IntPtr)MemoryRead(pBaseAddr + 0xF4, g_ahD2Handle, "word");
				IntPtr iName = (IntPtr)mainInstance.RemoteThread(mainInstance.g_pD2InjectGetString, iNameID);
				
				var rawBytes = MemoryRead(iName, g_ahD2Handle, new byte[200]);
				var rawString = Encoding.Unicode.GetString(rawBytes);
				var sName = rawString.Substring(0, rawString.IndexOf('\0'));

				sName = sName.Replace("\n", "|");
				sName = Regex.Replace(sName, "ÿc.", "");

				// == Get tier ==
				// = tier "0" is Amulet/Ring
				var sTier = "0";
				if (MemoryRead(pBaseAddr + 0x84, g_ahD2Handle) > 0) {
					// Weapon / Armor

					var match = Regex.Match(sName, "[1-4]|[(]Sacred[)]");
					if (match.Success) {
						sTier = match.Value.Equals("(Sacred)") ? "sacred" : match.Value;
					}
				}

				var cacheItem = new CacheItem() {
					FullText = sName,
					Tier = ItemFlags.GetFlagId(sTier),
					Name = Regex.Replace(sName, ".+\\|", "")
				};
				ItemCache.Add(cacheItem);
			}

			MainWindow.mainInstance.InitItemAutocomplete(ItemCache);

			return true;
		}

		public class CacheItem
		{
			public string FullText { get; set; }
			public int Tier { get; set; }
			public string Name { get; set; }

			public override string ToString()
			{
				return FullText;
			}
		}

		public void UpdateMatchList()
		{
			if (!NeedUpdateList) {
				return;
			}
			NeedUpdateList = false;
			mainInstance.g_bnotifierChanged = true;

			var asLines = Settings.Default.notifierText.Split('\n');
			var iLines = asLines.Length;

			notifyMatchList = new List<NotifyMatch>();
			var tempMatchErrorLines = new ConcurrentBag<int>();

			for (int i = 0; i < iLines; i++) {
				try {
					NotifyMatch notifyMatch = NotifyMatch.FromString(asLines[i]);
					if (notifyMatch != null) {
						notifyMatchList.Add(notifyMatch);
					}
				} catch (Exception) {
					tempMatchErrorLines.Add(i);
					logger.Debug($"Warning: NotifierUpdateList() - Could not parse line {i}");
				}
			}

			MatchErrorLines = tempMatchErrorLines;
		}

		public List<string> FindItemsFromNotifierString(string notifierString)
		{
			if (InitItemCache() == false) {
				return new List<string>() { "Error updating NotifierCache - Probably not ingame" };
			}

			List<string> asMatches = new List<string>();

			NotifyMatch testMatch = NotifyMatch.FromString(notifierString);

			if (testMatch != null) {
				var sMatch = testMatch.Match;
				var iFlagsTier = testMatch.Flags[ItemFlagGroup.Tier];

				for (int i = 0; i < ItemCache.Count; i++) {

					var sName = ItemCache[i].FullText;
					var iTierFlag = ItemCache[i].Tier;

					if (Regex.IsMatch(sName, sMatch)) {
						if (iFlagsTier != 0 && (iFlagsTier & iTierFlag) == 0) { continue; }

						asMatches.Add(ItemCache[i].Name);
					}
				}
			}
			return asMatches;
		}

		class ItemFlags
		{
			static Dictionary<ItemFlagGroup, string[]> notifyFlags = new Dictionary<ItemFlagGroup, string[]>();

			static ItemFlags()
			{
				notifyFlags.Add(ItemFlagGroup.Tier, new [] { "0", "1", "2", "3", "4", "sacred" });
				notifyFlags.Add(ItemFlagGroup.Quality, new [] { "low", "normal", "superior", "magic", "set", "rare", "unique", "craft", "honor" });
				notifyFlags.Add(ItemFlagGroup.Misc, new [] { "eth", "socket" });
				notifyFlags.Add(ItemFlagGroup.NoMask, new [] { "" });
				notifyFlags.Add(ItemFlagGroup.Colour, new [] { "clr_none", "white", "red", "lime", "blue", "gold", "grey", "black", "clr_unk", "orange", "yellow", "green", "purple", "show", "hide" });
				notifyFlags.Add(ItemFlagGroup.Sound, new [] { "sound1", "sound2", "sound3", "sound4", "sound5" });
				notifyFlags.Add(ItemFlagGroup.Match, new [] { "" });
			}

			public static int GetFlagId(string flagString)
			{
				var groupAndIndex = ParseFlagIndex(flagString);
				if (groupAndIndex.Group == ItemFlagGroup.Last) {
					throw new Exception("Error with NotifierFlag");
				}

				return groupAndIndex.Group > ItemFlagGroup.NoMask ? groupAndIndex.Flag : 1 << groupAndIndex.Flag;
			}

			public static (ItemFlagGroup Group, int Flag) ParseFlagIndex(string flagString)
			{
				for (ItemFlagGroup group = 0; group < ItemFlagGroup.Last; group++) {
					var flags = notifyFlags[group];

					for (int flagIndex = 0; flagIndex < flags.Length; flagIndex++) {

						if (flags[flagIndex].Length == 0) {
							break;
						} else if (flags[flagIndex].Equals(flagString)) {
							return (Group: group, Flag: flagIndex);
						}
					}
				}

				// Group/Flag not found.
				return (ItemFlagGroup.Last, Flag: -1);
			}
		}

		/// <summary>
		/// Example: "Greaves" 4 sound1
		/// Match: "Greaves"
		/// Flags: "4", "sound1"
		/// </summary>
		public class NotifyMatch
		{
			public int Tier { get { return Flags[ItemFlagGroup.Tier]; } }
			public int Quality { get { return Flags[ItemFlagGroup.Quality]; } }
			public int Misc { get { return Flags[ItemFlagGroup.Misc]; } }
			public int NoMask { get { return Flags[ItemFlagGroup.NoMask]; } }
			public int Colour { get { return Flags[ItemFlagGroup.Colour]; } }
			public int Sound { get { return Flags[ItemFlagGroup.Sound]; } }

			public Dictionary<ItemFlagGroup, int> Flags = new Dictionary<ItemFlagGroup, int>() {
				{ ItemFlagGroup.Tier, 0 },
				{ ItemFlagGroup.Quality, 0 },
				{ ItemFlagGroup.Misc, 0 },
				{ ItemFlagGroup.NoMask, 0 },
				{ ItemFlagGroup.Colour, 0 },
				{ ItemFlagGroup.Sound, -1 }
			};

			public string Match = "";

			string orgString;
			string cleanString;

			public static NotifyMatch FromString(string line)
			{
				var matchLine = new NotifyMatch();
				if (matchLine.Parse(line) == false) {
					return null;
				}

				return matchLine;
			}

			internal bool Parse(string line)
			{
				orgString = line;

				cleanString = CleanString(line);
				
				return ParseClean(cleanString);
			}

			internal string CleanString(string line)
			{
				// Remove Comments
				line = Regex.Replace(line, "#.*", "");

				line = line.Trim();
				// Remove 2 or more connected spaces
				line = Regex.Replace(line, "[ ]{2,}", "");

				return line;
			}

			internal bool ParseClean(string line)
			{
				string stringBuffer = "";
				bool bQuoted = false;
				bool bHasFlags = false;

				for (int i = 0; i < line.Length; i++) {
					var sChar = line[i];

					if (sChar == '"') {
						if (bQuoted) {
							Match = stringBuffer;
							stringBuffer = "";
						}

						bQuoted = !bQuoted;
					} else if (sChar == ' ' && !bQuoted) {
						if (ParseFlag(stringBuffer)) { bHasFlags = true; }
						stringBuffer = "";
					} else {
						stringBuffer += sChar;
					}
				}

				if (ParseFlag(stringBuffer)) {
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

				var groupAndFlag = ItemFlags.ParseFlagIndex(sFlag);
				if (groupAndFlag.Group == ItemFlagGroup.Last) {
					throw new Exception($"Unknown notifier flag '{sFlag}' in line:\r\n{orgString}");
					return false;
				}

				if (groupAndFlag.Group < ItemFlagGroup.NoMask) {
					// $iFlag = BitOR(BitRotate(1, $iFlag, "D"), $avRet[$iGroup])
					groupAndFlag.Flag = (1 << groupAndFlag.Flag) | Flags[groupAndFlag.Group];
				}
				Flags[groupAndFlag.Group] = groupAndFlag.Flag;
				//notifyCompile.SetValue((enotifyFlags)iGroup, iFlag.ToString());

				return groupAndFlag.Group != ItemFlagGroup.Colour;
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

		public enum ItemFlagGroup
		{
			Tier, Quality, Misc, NoMask, Colour, Sound, Match, Last
		}

		public enum ItemQuality
		{
			None, Low, Normal, Superior, Magic, Set, Rare, Unique, Craft, Honorific
		}
	}
}
