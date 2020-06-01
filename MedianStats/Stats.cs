using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MedianStats.MainWindow;
using static MedianStats.NomadMemory;

namespace MedianStats
{
	public class Stats
	{
		private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public IntPtr g_hD2Client { get { return mainInstance.g_hD2Client; } }
		public IntPtr g_ahD2Handle { get { return mainInstance.g_ahD2Handle; } }
		public IntPtr g_hD2Common { get { return mainInstance.g_hD2Common; } }
		public IntPtr g_pD2sgpt { get { return mainInstance.g_pD2sgpt; } }


		const int g_iNumStats = 1024;

		int[,] g_aiStatsCache = new int[2, g_iNumStats];

		public void UpdateStatValues()
		{
			for (int i = 0; i < g_iNumStats; i++) {
				g_aiStatsCache[0, i] = 0;
				g_aiStatsCache[1, i] = 0;
			}

			if (mainInstance.IsIngame()) {
				UpdateStatValueMem(0);
				UpdateStatValueMem(1);
				FixStats();
				FixVeteranToken();
				CalculateWeaponDamage();

				// Poison damage to damage/second
				g_aiStatsCache[1, 57] *= (25 / 256);
				g_aiStatsCache[1, 58] *= (25 / 256);

				// Bonus stats from items; str, dex, vit, ene
				int[] aiStats = new int[] { 0, 359, 2, 360, 3, 362, 1, 361 };
				int iBase, iTotal, iPercent;

				for (int i = 0; i <= 3; i++) {
					iBase = GetStatValue(aiStats[i * 2 + 0]);
					iTotal = GetStatValue(aiStats[i * 2 + 0], 1);
					iPercent = GetStatValue(aiStats[i * 2 + 1]);

					g_aiStatsCache[1, 900 + i] = /*Ceiling*/(iTotal / (1 + iPercent / 100) - iBase);
				}

				// Factor cap
				var iFactor = (int)Math.Floor((GetStatValue(278) * GetStatValue(0, 1) + GetStatValue(485) * GetStatValue(1, 1)) / 3e6 * 100);
				g_aiStatsCache[1, 904] = iFactor > 100 ? 100 : iFactor;
			}
		}

		public IntPtr GetUnitToRead()
		{
			var bMercenary = mainInstance.readMercenary.IsChecked.Value;
			return g_hD2Client + (bMercenary ? 0x10A80C : 0x11BBFC);
		}

		public struct tagStat
		{
			//word wSubIndex;word wStatIndex;int dwStatValue;
			public short wSubIndex;
			public short wStatIndex;
			public int dwStatValue;
		}

		public void UpdateStatValueMem(int iVector)
		{
			if (iVector !=/*<>*/ 0 && iVector !=/*<>*/ 1) {
				logger.Debug("UpdateStatValueMem: Invalid iVector value.");
			}

			var pUnitAddress = GetUnitToRead();

			var aiOffsets = new int[] { 0, 0x5C, (iVector + 1) * 0x24 };
			var pStatList = MemoryPointerRead(pUnitAddress, g_ahD2Handle, aiOffsets);

			aiOffsets[2] += 0x4;
			var iStatCount = (ushort)MemoryPointerRead(pUnitAddress, g_ahD2Handle, aiOffsets, "word") - 1;

			//var tagStat = "word wSubIndex;word wStatIndex;int dwStatValue;";
			//string tagStatsAll = "";
			//for (int i = 0; i <= (int)iStatCount; i++) {
			//	tagStatsAll += tagStat;
			//}

			//var tStats = DllStructCreate(tagStatsAll);
			//_WinAPI_ReadProcessMemory(g_ahD2Handle[1], pStatList, DllStructGetPtr(tStats), DllStructGetSize(tStats), 0);
			var tStats = ReadProcessMemoryStructArray<tagStat>(g_ahD2Handle, pStatList, iStatCount);

			int iStatIndex, iStatValue;

			for (int i = 0; i < iStatCount; i++) {
				//iStatIndex = DllStructGetData(tStats, 2 + (3 * i));
				iStatIndex = tStats[i].wStatIndex;
				if (iStatIndex >= g_iNumStats) {
					continue; // Should never happen
				}

				//iStatValue = DllStructGetData(tStats, 3 + (3 * i));
				iStatValue = tStats[i].dwStatValue;

				switch (iStatIndex) {
					case var n when (n >= 6 && n <= 11):
						g_aiStatsCache[iVector, iStatIndex] += iStatValue / 256;
						break;
					default:
						g_aiStatsCache[iVector, iStatIndex] += iStatValue;
						break;
				}
			}
		}

		public IntPtr GetUnitWeapon(IntPtr pUnit)
		{
			var pInventory = (IntPtr)MemoryRead(pUnit + 0x60, g_ahD2Handle);

			var pItem = (IntPtr)MemoryRead(pInventory + 0x0C, g_ahD2Handle);
			var iWeaponID = MemoryRead(pInventory + 0x1C, g_ahD2Handle);

			IntPtr pItemData = IntPtr.Zero;
			IntPtr pWeapon = IntPtr.Zero;

			while (pItem != (IntPtr)0) {
				if (iWeaponID == MemoryRead(pItem + 0x0C, g_ahD2Handle)) {
					pWeapon = pItem;
					break;
				}

				pItemData = (IntPtr)MemoryRead(pItem + 0x14, g_ahD2Handle);
				pItem = (IntPtr)MemoryRead(pItemData + 0x64, g_ahD2Handle);
			}

			return pWeapon;
		}

		public void CalculateWeaponDamage()
		{
			var pUnitAddress = GetUnitToRead();
			var pUnit = (IntPtr)MemoryRead(pUnitAddress, g_ahD2Handle);

			var pWeapon = GetUnitWeapon(pUnit);
			if (pWeapon /*not*/ == IntPtr.Zero) { return; }

			var iWeaponClass = MemoryRead(pWeapon + 0x04, g_ahD2Handle);
			var pItemsTxt = (IntPtr)MemoryRead(g_hD2Common + 0x9FB98, g_ahD2Handle);
			var pBaseAddr = pItemsTxt + 0x1A8 * iWeaponClass;

			var iStrBonus = MemoryRead(pBaseAddr + 0x106, g_ahD2Handle, "word");
			var iDexBonus = MemoryRead(pBaseAddr + 0x108, g_ahD2Handle, "word");
			bool bIs2H = MemoryRead(pBaseAddr + 0x11C, g_ahD2Handle, "byte") != 0;
			bool bIs1H = bIs2H ? MemoryRead(pBaseAddr + 0x13D, g_ahD2Handle, "byte") != 0 : true;

			int iMinDamage1 = 0, iMinDamage2 = 0, iMaxDamage1 = 0, iMaxDamage2 = 0;

			if (bIs2H) {
				//; 2h weapon
				iMinDamage2 = GetStatValue(23);
				iMaxDamage2 = GetStatValue(24);
			}

			if (bIs1H) {
				//; 1h weapon
				iMinDamage1 = GetStatValue(21);
				iMaxDamage1 = GetStatValue(22);

				if (!/*not*/ bIs2H) {
					// thrown weapon
					iMinDamage2 = GetStatValue(159);
					iMaxDamage2 = GetStatValue(160);
				}
			}

			if (iMaxDamage1 < iMinDamage1) { iMaxDamage1 = iMinDamage1 + 1; }
			if (iMaxDamage2 < iMinDamage2) { iMaxDamage2 = iMinDamage2 + 1; }

			var iStatBonus = /*Floor*/((GetStatValue(0, 1) * iStrBonus + GetStatValue(2, 1) * iDexBonus) / 100) - 1;
			var iEWD = GetStatValue(25) + GetStatValue(343); // global EWD, itemtype-specific EWD
			var fTotalMult = 1 + iEWD / 100 + iStatBonus / 100;

			int[] aiDamage = new int[] { iMinDamage1, iMaxDamage1, iMinDamage2, iMaxDamage2 };

			for (int i = 0; i <= 3; i++) {
				g_aiStatsCache[1, 21 + i] = /*Floor*/(aiDamage[i] * fTotalMult);
			}
		}

		// This game is stupid
		public void FixStats()
		{
			// Velocities
			for (int i = 67; i <= 69; i++) {
				g_aiStatsCache[1, i] = 0;
			}

			// itemtype-specific EWD (Elfin Weapons, Shadow Dancer)
			g_aiStatsCache[1, 343] = 0;

			var pSkillsTxt = (IntPtr)MemoryRead(g_pD2sgpt + 0xB98, g_ahD2Handle);
			int iSkillID, iStatCount, iStatIndex, iStatValue, iOwnerType, iStateID;
			IntPtr pStats, pSkill;

			var pItemTypesTxt = (IntPtr)MemoryRead(g_pD2sgpt + 0xBF8, g_ahD2Handle);
			var pItemsTxt = (IntPtr)MemoryRead(g_hD2Common + 0x9FB98, g_ahD2Handle);
			int iWeaponClass, iWeaponType, iItemType;

			var pUnitAddress = GetUnitToRead();
			var pUnit = (IntPtr)MemoryRead(pUnitAddress, g_ahD2Handle);

			var aiOffsets = new int[] { 0, 0x5C, 0x3C };
			var pStatList = MemoryPointerRead(pUnitAddress, g_ahD2Handle, aiOffsets);

			while (pStatList != (IntPtr)0) {
				iOwnerType = MemoryRead(pStatList + 0x08, g_ahD2Handle);
				pStats = (IntPtr)MemoryRead(pStatList + 0x24, g_ahD2Handle);
				iStatCount = MemoryRead(pStatList + 0x28, g_ahD2Handle, "word");
				pStatList = (IntPtr)MemoryRead(pStatList + 0x2C, g_ahD2Handle);

				iSkillID = 0;

				for (int i = 0; i < iStatCount; i++) {

					iStatIndex = MemoryRead(pStats + i * 8 + 2, g_ahD2Handle, "word");
					iStatValue = MemoryRead(pStats + i * 8 + 4, g_ahD2Handle, /*"int"*/"dword");

					if (iStatIndex == 350 && iStatValue !=/*<>*/ 511) { iSkillID = iStatValue; }
					if (iOwnerType == 4 && iStatIndex == 67) { g_aiStatsCache[1, iStatIndex] += iStatValue; } // Armor FRW penalty
				}

				if (iOwnerType == 4) { continue; }

				iStateID = MemoryRead(pStatList + 0x14, g_ahD2Handle);
				switch (iStateID) {
					case 195: // Dark Power, Tome of Possession aura
						iSkillID = 687; //Dark Power
						break;
				}

				var bHasVelocity = new bool[] { false, false, false };
				if (iSkillID != 0) { //; Game doesn't even bother setting the skill id for some skills, so we'll just have to hope the state is correct or the stat list isn't lying...
					pSkill = pSkillsTxt + 0x23C * iSkillID;

					for (int i = 0; i <= 4; i++) {
						iStatIndex = MemoryRead(pSkill + 0x98 + i * 2, g_ahD2Handle, "word");

						switch (iStatIndex) {
							case var n when (n >= 67 && n <= 69):
								bHasVelocity[iStatIndex - 67] = true;
								break;
						}
					}

					for (int i = 0; i <= 5; i++) {

						iStatIndex = MemoryRead(pSkill + 0x54 + i * 2, g_ahD2Handle, "word");

						switch (iStatIndex) {
							case var n when (n >= 67 && n <= 69):
								bHasVelocity[iStatIndex - 67] = true;
								break;
						}
					}
				}

				for (int i = 0; i < iStatCount; i++) {

					iStatIndex = MemoryRead(pStats + i * 8 + 2, g_ahD2Handle, "word");
					iStatValue = MemoryRead(pStats + i * 8 + 4, g_ahD2Handle, /*"int"*/"dword");

					switch (iStatIndex) {
						case var n when (n >= 67 && n <= 69):
							if (0 ==/*not*/ iSkillID || /*or*/bHasVelocity[iStatIndex - 67]) { g_aiStatsCache[1, iStatIndex] += iStatValue; }
							break;
						case 343:
							iItemType = MemoryRead(pStats + i * 8 + 0, g_ahD2Handle, "word");
							var pWeapon = GetUnitWeapon(pUnit);
							if (pWeapon /*not*/ == IntPtr.Zero ||/*or*/ iItemType /*not*/ == 0) { continue; }

							iWeaponClass = MemoryRead(pWeapon + 0x04, g_ahD2Handle);
							iWeaponType = MemoryRead(pItemsTxt + 0x1A8 * iWeaponClass + 0x11E, g_ahD2Handle, "word");

							bool bApply = false;
							//var aiItemTypes[256] = [1, iWeaponType];
							// Bin nicht sicher ob das so richtig ist wie ichs in C# nachbaue:
							int[] aiItemTypes = new int[256];
							aiItemTypes[0] = 1;
							aiItemTypes[0] = iWeaponType;

							int iEquiv;
							var j = 1;

							while (j <= aiItemTypes[0]) {
								if (aiItemTypes[j] == iItemType) {
									bApply = true;
									break;
								}
								for (int k = 0; k <= 1; k++) {
									iEquiv = MemoryRead(pItemTypesTxt + 0xE4 * aiItemTypes[j] + 0x04 + k * 2, g_ahD2Handle, "word");
									if (iEquiv != 0) {
										aiItemTypes[0] += 1;
										aiItemTypes[aiItemTypes[0]] = iEquiv;
									}
								}

								j += 1;
							}

							if (bApply) { g_aiStatsCache[1, 343] += iStatValue; }
							break;
					}
				}
			}
		}

		public void FixVeteranToken()
		{
			g_aiStatsCache[1, 219] = 0; //; Veteran token

			var pUnitAddress = GetUnitToRead();

			int[] aiOffsets = { 0, 0x60, 0x0C };
			var pItem = MemoryPointerRead(pUnitAddress, g_ahD2Handle, aiOffsets);

			IntPtr pItemData, pStatsEx, pStats;
			int iStatCount, iStatIndex, iVeteranTokenCounter;

			while (pItem != IntPtr.Zero) {
				pItemData = (IntPtr)MemoryRead(pItem + 0x14, g_ahD2Handle);
				pStatsEx = (IntPtr)MemoryRead(pItem + 0x5C, g_ahD2Handle);
				pItem = (IntPtr)MemoryRead(pItemData + 0x64, g_ahD2Handle);

				if (pStatsEx /*not*/ == IntPtr.Zero) { continue; }

				pStats = (IntPtr)MemoryRead(pStatsEx + 0x48, g_ahD2Handle);
				if (pStats /*not*/ == IntPtr.Zero) { continue; }

				iStatCount = MemoryRead(pStatsEx + 0x4C, g_ahD2Handle, "word");
				iVeteranTokenCounter = 0;

				for (int i = 0; i < iStatCount; i++) {
					iStatIndex = MemoryRead(pStats + i * 8 + 2, g_ahD2Handle, "word");

					switch (iStatIndex) {
						case 83:
						case 85:
						case 219:
							iVeteranTokenCounter += 1;
							break;
					}
				}

				if (iVeteranTokenCounter == 3) {
					g_aiStatsCache[1, 219] = 1; //Veteran token
					return;
				}
			}
		}

		public int GetStatValue(int iStatID)
		{
			int iVector = iStatID < 4 ? 0 : 1;
			return GetStatValue(iStatID, iVector);
		}

		public int GetStatValue(int iStatID, int iVector)
		{
			var iStatValue = g_aiStatsCache[iVector, iStatID];
			//return /*Floor*/(iStatValue != 0 ? iStatValue : 0);
			return iStatValue;
		}
	}
}
