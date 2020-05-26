using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MedianStats
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct UnitAny
	{
		//"dword iUnitType;dword iClass;dword pad1[3];dword pUnitData;dword pad2[52];dword pUnit;"
		[MarshalAs(UnmanagedType.U4)]
		public /*dword*/uint iUnitType;
		[MarshalAs(UnmanagedType.U4)]
		public /*dword*/uint iClass;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		public /*dword*/uint[] pad1/*[3]*/;
		[MarshalAs(UnmanagedType.U4)]
		public /*dword*/uint pUnitData;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 52)]
		public /*dword*/uint[] pad2/*[52]*/;
		[MarshalAs(UnmanagedType.U4)]
		public /*dword*/uint pUnitNext;

		const uint unitTypeItem = 4;

		public bool IsItem { get { return iUnitType == unitTypeItem; } }
		public string FullText { get { return Notifier.ItemCache[(int)iClass].FullText; } }
		public int TierFlag { get { return Notifier.ItemCache[(int)iClass].Tier; } }
		public string Name { get { return Notifier.ItemCache[(int)iClass].Name; } }
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct ItemData
	{
		//"dword iQuality;dword pad1[5];dword iFlags;dword pad2[11];byte iEarLevel;"
		[MarshalAs(UnmanagedType.U4)]
		public /*dword*/uint iQuality;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
		public /*dword*/uint[] pad1/*[5]*/;
		[MarshalAs(UnmanagedType.U4)]
		public /*dword*/uint iFlags;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
		public /*dword*/uint[] pad2/*[11]*/;
		[MarshalAs(UnmanagedType.U1)]
		public byte iEarLevel;
	}
}
