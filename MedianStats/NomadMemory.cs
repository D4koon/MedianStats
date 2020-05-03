using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static MedianStats.Util;

namespace MedianStats
{
	public class NomadMemory
	{
		[Flags]
		public enum ProcessAccessFlags : uint
		{
			All = 0x001F0FFF,
			Terminate = 0x00000001,
			CreateThread = 0x00000002,
			VirtualMemoryOperation = 0x00000008,
			VirtualMemoryRead = 0x00000010,
			VirtualMemoryWrite = 0x00000020,
			DuplicateHandle = 0x00000040,
			CreateProcess = 0x000000080,
			SetQuota = 0x00000100,
			SetInformation = 0x00000200,
			QueryInformation = 0x00000400,
			QueryLimitedInformation = 0x00001000,
			Synchronize = 0x00100000
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr OpenProcess(
			 ProcessAccessFlags processAccess,
			 bool bInheritHandle,
			 int processId
		);

		public static IntPtr _WinAPI_OpenProcess(Process proc, ProcessAccessFlags flags)
		{
			 return OpenProcess(flags, false, proc.Id);
		}



		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ReadProcessMemory(
			IntPtr hProcess,
			IntPtr lpBaseAddress,
			byte[] lpBuffer,
			Int32 nSize,
			out int lpNumberOfBytesRead);

		public static T ReadProcessMemoryStruct<T>(IntPtr processHandle, IntPtr address, int structSize = 0)
		{
			if (structSize == 0)
				structSize = Marshal.SizeOf(typeof(T));
			byte[] bytes = new byte[structSize];
			int numRead = 0;
			if (!ReadProcessMemory(processHandle, address, bytes, bytes.Length, out numRead))
				throw new Exception("ReadProcessMemory failed");
			if (numRead != bytes.Length)
				throw new Exception("Number of bytes read does not match structure size");
			GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
			handle.Free();
			return structure;
		}

		public static T[] ReadProcessMemoryStructArray<T>(IntPtr processHandle, IntPtr address, int arrayLength)
		{
			int structSize = Marshal.SizeOf(typeof(T));
			byte[] bytes = new byte[structSize * arrayLength];
			int numRead = 0;
			if (!ReadProcessMemory(processHandle, address, bytes, bytes.Length, out numRead)) {
				throw new Exception("ReadProcessMemory failed");
			}
			if (numRead != bytes.Length) {
				throw new Exception("Number of bytes read does not match structure size");
			}

			T[] structure = new T[arrayLength];

			byte[] buffer = new byte[structSize];
			int structureIndex = 0;
			for (int i = 0; i < bytes.Length; i += structSize) {
				Buffer.BlockCopy(bytes, i, buffer, 0, structSize);

				GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
				structure[structureIndex] = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
				handle.Free();

				structureIndex++;
			}

			return structure;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool WriteProcessMemory(
			IntPtr hProcess,
			IntPtr lpBaseAddress,
			byte[] lpBuffer,
			Int32 nSize,
			out IntPtr lpNumberOfBytesWritten);



		/*#include-once*/

		/*
		;=================================================================================================
		; AutoIt Version:	3.1.127 (beta)
		; Language:			English
		; Platform:			All Windows
		; Author:			Nomad
		; Requirements:		These functions will only work with beta.
		;=================================================================================================
		; Credits:	wOuter - These functions are based on his original _Mem() functions.  But they are
		;			easier to comprehend and more reliable.  These functions are in no way a direct copy
		;			of his functions.  His functions only provided a foundation from which these evolved.
		;=================================================================================================
		;
		; Functions:
		;
		;=================================================================================================
		; Function:			_MemoryOpen($iv_Pid[, $iv_DesiredAccess[, $iv_InheritHandle]])
		; Description:		Opens a process and enables all possible access rights to the process.  The
		;					Process ID of the process is used to specify which process to open.  You must
		;					call this function before calling _MemoryClose(), _MemoryRead(), or _MemoryWrite().
		; Parameter(s):		$iv_Pid - The Process ID of the program you want to open.
		;					$iv_DesiredAccess - (optional) Set to 0x1F0FFF by default, which enables all
		;										possible access rights to the process specified by the
		;										Process ID.
		;					$if_InheritHandle - (optional) If this value is TRUE, all processes created by
		;										this process will inherit the access handle.  Set to TRUE
		;										(1) by default.  Set to 0 if you want it to be FALSE.
		; Requirement(s):	A valid process ID.
		; Return Value(s): 	On Success - Returns an array containing the Dll handle and an open handle to
		;								 the specified process.
		;					On Failure - Returns 0
		;					@Error - 0 = No error.
		;							 1 = Invalid $iv_Pid.
		;							 2 = Failed to open Kernel32.dll.
		;							 3 = Failed to open the specified process.
		; Author(s):		Nomad
		; Note(s):
		;=================================================================================================*/
		public static IntPtr _MemoryOpen(int iv_Pid, int iv_DesiredAccess = 0x1F0FFF, bool if_InheritHandle = true)
		{
			//if (ProcessExists(iv_Pid) == false) {  /*Then*/
			//	SetError(1);
		 //       return 0;
			//}  /*EndIf*/
	
			//var ah_Handle[2] = [DllOpen('kernel32.dll')];
	
			//if (@Error) {  /*Then*/
		 //       SetError(2);

			//	return 0;
		 //   }  /*EndIf*/
	
			//; Local $av_OpenProcess = DllCall(ah_Handle[0], 'int', 'OpenProcess', 'int', iv_DesiredAccess, 'int', if_InheritHandle, 'int', iv_Pid)
			var av_OpenProcess = OpenProcess((ProcessAccessFlags)iv_DesiredAccess, if_InheritHandle, iv_Pid/*, true*/);
	
			//if (@Error) {  /*Then*/
			//	DllClose(ah_Handle[0]);

			//	SetError(3);

			//	return 0;
			//}  /*EndIf*/

			IntPtr ah_Handle/*[1]*/ = av_OpenProcess;
	
			return ah_Handle;
	
		}

		/*;=================================================================================================
		; Function:			_MemoryRead($iv_Address, $ah_Handle[, $sv_Type])
		; Description:		Reads the value located in the memory address specified.
		; Parameter(s):		$iv_Address - The memory address you want to read from. It must be in hex
		;								  format (0x00000000).
		;					$ah_Handle - An array containing the Dll handle and the handle of the open
		;								 process as returned by _MemoryOpen().
		;					$sv_Type - (optional) The "Type" of value you intend to read.  This is set to
		;								'dword'(32bit(4byte) signed integer) by default.  See the help file
		;								for DllStructCreate for all types.
		;								An example: If you want to read a word that is 15 characters in
		;								length, you would use 'char[16]'.
		; Requirement(s):	The $ah_Handle returned from _MemoryOpen.
		; Return Value(s):	On Success - Returns the value located at the specified address.
		;					On Failure - Returns 0
		;					@Error - 0 = No error.
		;							 1 = Invalid $ah_Handle.
		;							 2 = $sv_Type was not a string.
		;							 3 = $sv_Type is an unknown data type.
		;							 4 = Failed to allocate the memory needed for the DllStructure.
		;							 5 = Error allocating memory for $sv_Type.
		;							 6 = Failed to read from the specified process.
		; Author(s):		Nomad
		; Note(s):			Values returned are in Decimal format, unless specified as a 'char' type, then
		;					they are returned in ASCII format.  Also note that size ('char[size]') for all
		;					'char' types should be 1 greater than the actual size.
		;=================================================================================================*/

		public static int _MemoryRead(IntPtr iv_Address, IntPtr ah_Handle, string sv_Type = "dword")
		{
			//if (IsArray(ah_Handle) == false) {  /*Then*/
			//	//SetError(1);
			//       return 0;
			//}  /*EndIf*/

			var v_Buffer = AutoItApi.DllStructCreate(sv_Type);

			//if (@Error) {  /*Then*/
			//	//SetError(@Error + 1);
			//	return 0;
			//}  /*EndIf*/
	
			/*DllCall(ah_Handle[0], 'int', 'ReadProcessMemory', 'int', ah_Handle[1], 'int', iv_Address, 'ptr', DllStructGetPtr(v_Buffer), 'int', DllStructGetSize(v_Buffer), 'int', '')*/
			int numberBytesRead = 0;
			ReadProcessMemory(ah_Handle/*[1]*/, iv_Address, v_Buffer, AutoItApi.DllStructGetSize(v_Buffer), out numberBytesRead);
	
			//if (Not @Error) {  /*Then*/
				var v_Value = AutoItApi.DllStructGetData(v_Buffer, 1, sv_Type);
				return v_Value;
			//} else {
			//	//SetError(6);

			//	return 0;
			//}  /*EndIf*/
	
		}

		/// <summary>
		/// NOTE: Not original! von mir estellt
		/// </summary>
		public static byte[] _MemoryRead(IntPtr iv_Address, IntPtr ah_Handle, byte[] v_Buffer)
		{
			//if (IsArray(ah_Handle) == false) {  /*Then*/
			//	//SetError(1);
			//       return 0;
			//}  /*EndIf*/

			//var v_Buffer = AutoItApi.DllStructCreate(sv_Type);

			//if (@Error) {  /*Then*/
			//	//SetError(@Error + 1);
			//	return 0;
			//}  /*EndIf*/

			/*DllCall(ah_Handle[0], 'int', 'ReadProcessMemory', 'int', ah_Handle[1], 'int', iv_Address, 'ptr', DllStructGetPtr(v_Buffer), 'int', DllStructGetSize(v_Buffer), 'int', '')*/
			int numberBytesRead = 0;
			ReadProcessMemory(ah_Handle/*[1]*/, iv_Address, v_Buffer, AutoItApi.DllStructGetSize(v_Buffer), out numberBytesRead);

			//if (Not @Error) {  /*Then*/
			//var v_Value = AutoItApi.DllStructGetData(v_Buffer, 1);
			return v_Buffer;
			//} else {
			//	//SetError(6);

			//	return 0;
			//}  /*EndIf*/

		}

		/*;=================================================================================================
		; Function:			_MemoryWrite($iv_Address, $ah_Handle, $v_Data[, $sv_Type])
		; Description:		Writes data to the specified memory address.
		; Parameter(s):		$iv_Address - The memory address you want to write to.  It must be in hex
		;								  format (0x00000000).
		;					$ah_Handle - An array containing the Dll handle and the handle of the open
		;								 process as returned by _MemoryOpen().
		;					$v_Data - The data to be written.
		;					$sv_Type - (optional) The "Type" of value you intend to write.  This is set to
		;								'dword'(32bit(4byte) signed integer) by default.  See the help file
		;								for DllStructCreate for all types.
		;								An example: If you want to write a word that is 15 characters in
		;								length, you would use 'char[16]'.
		; Requirement(s):	The $ah_Handle returned from _MemoryOpen.
		; Return Value(s):	On Success - Returns 1
		;					On Failure - Returns 0
		;					@Error - 0 = No error.
		;							 1 = Invalid $ah_Handle.
		;							 2 = $sv_Type was not a string.
		;							 3 = $sv_Type is an unknown data type.
		;							 4 = Failed to allocate the memory needed for the DllStructure.
		;							 5 = Error allocating memory for $sv_Type.
		;							 6 = $v_Data is not in the proper format to be used with the "Type"
		;								 selected for $sv_Type, or it is out of range.
		;							 7 = Failed to write to the specified process.
		; Author(s):		Nomad
		; Note(s):			Values sent must be in Decimal format, unless specified as a 'char' type, then
		;					they must be in ASCII format.  Also note that size ('char[size]') for all
		;					'char' types should be 1 greater than the actual size.
		;=================================================================================================*/
		public static void _MemoryWrite(IntPtr iv_Address, IntPtr ah_Handle, string v_Data, string sv_Type = "dword")
		{
			_MemoryWrite(iv_Address, ah_Handle, StringToByteArray(v_Data), sv_Type);
		}

		public static void _MemoryWrite(IntPtr iv_Address, IntPtr ah_Handle, byte[] v_Data, string sv_Type = "dword")
		{	
			//If Not IsArray(ah_Handle) {  /*Then*/
			//	SetError(1)
			//	Return 0
			//}  /*EndIf*/
	
			var v_Buffer = AutoItApi.DllStructCreate(sv_Type);

			//If @Error {  /*Then*/
			//	SetError(@Error + 1)
			//	Return 0
			//Else
				AutoItApi.DllStructSetData(v_Buffer, 1, v_Data);
				//If @Error {  /*Then*/
				//	SetError(6);
				//	return 0;
				//}  /*EndIf*/
			//}  /*EndIf*/

			/*DllCall(ah_Handle[0], 'int', 'WriteProcessMemory', 'int', ah_Handle[1], 'int', iv_Address, 'ptr', DllStructGetPtr(v_Buffer), 'int', DllStructGetSize(v_Buffer), 'int', '');*/
			IntPtr lpNumberOfBytesWritten;
			WriteProcessMemory(ah_Handle, iv_Address, v_Buffer, v_Buffer.Length, out lpNumberOfBytesWritten);
	
			//If Not @Error {  /*Then*/
			//	Return 1
			//Else
			//	SetError(7)
			//	Return 0
			//}  /*EndIf*/
		}

		// "5368" => { 0x53, 0x68 }
		
		public static uint _MemoryWriteHexString(IntPtr iv_Address, IntPtr ah_Handle, string v_Data)
		{
			return _MemoryWrite(iv_Address, ah_Handle, HexStringToByteArray(v_Data));
		}

		public static void _MemoryWrite(IntPtr iv_Address, IntPtr ah_Handle, string v_Data)
		{
			_MemoryWrite(iv_Address, ah_Handle, StringToByteArray(v_Data));
		}

		public static uint _MemoryWrite(IntPtr iv_Address, IntPtr ah_Handle, byte[] v_Data)
		{
			IntPtr lpNumberOfBytesWritten;
			WriteProcessMemory(ah_Handle, iv_Address, v_Data, v_Data.Length, out lpNumberOfBytesWritten);
			return (uint)lpNumberOfBytesWritten;
		}

		private static byte[] StringToByteArray(string str)
		{
			var enc = new System.Text.UnicodeEncoding();
			return enc.GetBytes(str);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool CloseHandle(IntPtr hObject);

		/*;=================================================================================================
		; Function:			_MemoryClose($ah_Handle)
		; Description:		Closes the process handle opened by using _MemoryOpen().
		; Parameter(s):		$ah_Handle - An array containing the Dll handle and the handle of the open
		;								 process as returned by _MemoryOpen().
		; Requirement(s):	The $ah_Handle returned from _MemoryOpen.
		; Return Value(s):	On Success - Returns 1
		;					On Failure - Returns 0
		;					@Error - 0 = No error.
		;							 1 = Invalid $ah_Handle.
		;							 2 = Unable to close the process handle.
		; Author(s):		Nomad
		; Note(s):
		;=================================================================================================*/
		public static void _MemoryClose(IntPtr ah_Handle)
		{
			//If Not IsArray(ah_Handle) {  /*Then*/
			//	SetError(1)
			//	Return 0
			//}  /*EndIf*/

			//DllCall(ah_Handle[0], 'int', 'CloseHandle', 'int', ah_Handle[1])
			CloseHandle(ah_Handle);
			//If Not @Error {  /*Then*/
			//	DllClose(ah_Handle[0])
			//	Return 1
			//Else
			//	DllClose(ah_Handle[0])
			//	SetError(2)
			//	Return 0
			//}  /*EndIf*/
	
		}

		/*;=================================================================================================
		; Function:			_MemoryPointerRead ($iv_Address, $ah_Handle, $av_Offset[, $sv_Type])
		; Description:		Reads a chain of pointers and returns an array containing the destination
		;					address and the data at the address.
		; Parameter(s):		$iv_Address - The static memory address you want to start at. It must be in
		;								  hex format (0x00000000).
		;					$ah_Handle - An array containing the Dll handle and the handle of the open
		;								 process as returned by _MemoryOpen().
		;					$av_Offset - An array of offsets for the pointers.  Each pointer must have an
		;								 offset.  If there is no offset for a pointer, enter 0 for that
		;								 array dimension.
		;					$sv_Type - (optional) The "Type" of data you intend to read at the destination
		;								 address.  This is set to 'dword'(32bit(4byte) signed integer) by
		;								 default.  See the help file for DllStructCreate for all types.
		; Requirement(s):	The $ah_Handle returned from _MemoryOpen.
		; Return Value(s):	On Success - Returns an array containing the destination address and the value
		;								 located at the address.
		;					On Failure - Returns 0
		;					@Error - 0 = No error.
		;							 1 = $av_Offset is not an array.
		;							 2 = Invalid $ah_Handle.
		;							 3 = $sv_Type is not a string.
		;							 4 = $sv_Type is an unknown data type.
		;							 5 = Failed to allocate the memory needed for the DllStructure.
		;							 6 = Error allocating memory for $sv_Type.
		;							 7 = Failed to read from the specified process.
		; Author(s):		Nomad
		; Note(s):			Values returned are in Decimal format, unless a 'char' type is selected.
		;					Set $av_Offset like this:
		;					$av_Offset[0] = NULL (not used)
		;					$av_Offset[1] = Offset for pointer 1 (all offsets must be in Decimal)
		;					$av_Offset[2] = Offset for pointer 2
		;					etc...
		;					(The number of array dimensions determines the number of pointers)
		;=================================================================================================*/
		public static IntPtr _MemoryPointerRead (IntPtr iv_Address, IntPtr ah_Handle, int[] av_Offset, string sv_Type = "dword")
		{	
			// NOTE: Replaces block below.
			var iv_PointerCount = av_Offset.Length - 1;

			//if IsArray(av_Offset) {  /*Then*/
			//	if IsArray(ah_Handle) {  /*Then*/
			//		var iv_PointerCount = UBound(av_Offset) - 1;
			//	else
			//		SetError(2);
			//		return 0;
			//	}  /*EndIf*/
			//else
			//	SetError(1);
			//	return 0;
			//}  /*EndIf*/

			IntPtr[] iv_Data = new IntPtr[2];
			var v_Buffer = AutoItApi.DllStructCreate("dword");

			int numberBytesRead = 0;
			for (int i = 0; i <= iv_PointerCount; i++) {
		
				if (i == iv_PointerCount) {
					// Last Pointer

					v_Buffer = AutoItApi.DllStructCreate(sv_Type);
					//if @Error {  /*Then*/
					//	SetError(@Error + 2);
					//	return 0;
					//}  /*Endif*/
			
					iv_Address = iv_Data[1] + av_Offset[i];
					//DllCall(ah_Handle[0], 'int', 'ReadProcessMemory', 'int', ah_Handle[1], 'int', iv_Address, 'ptr', DllStructGetPtr(v_Buffer), 'int', DllStructGetSize(v_Buffer), 'int', '')
					ReadProcessMemory(ah_Handle, iv_Address, v_Buffer, AutoItApi.DllStructGetSize(v_Buffer), out numberBytesRead);
					//if @Error {  /*Then*/
					//	SetError(7);
					//	return 0;
					//}  /*Endif*/

					iv_Data[1] = (IntPtr)AutoItApi.DllStructGetData(v_Buffer, 1, sv_Type);
			
				} else if (i == 0) {
					// First Pointer

					//DllCall(ah_Handle[0], 'int', 'ReadProcessMemory', 'int', ah_Handle[1], 'int', iv_Address, 'ptr', DllStructGetPtr(v_Buffer), 'int', DllStructGetSize(v_Buffer), 'int', '')
					ReadProcessMemory(ah_Handle, iv_Address, v_Buffer, AutoItApi.DllStructGetSize(v_Buffer), out numberBytesRead);
					//if @Error {  /*Then*/
					//	SetError(7);
					//	return 0;
					//}  /*Endif*/

					// Pointer always dword
					iv_Data[1] = (IntPtr)AutoItApi.DllStructGetData(v_Buffer, 1, "dword");
			
				} else {
					iv_Address = iv_Data[1] + av_Offset[i];
					//DllCall(ah_Handle[0], 'int', 'ReadProcessMemory', 'int', ah_Handle[1], 'int', iv_Address, 'ptr', DllStructGetPtr(v_Buffer), 'int', DllStructGetSize(v_Buffer), 'int', '')
					ReadProcessMemory(ah_Handle, iv_Address, v_Buffer, AutoItApi.DllStructGetSize(v_Buffer), out numberBytesRead);
					//if @Error {  /*Then*/
					//	SetError(7);
					//	return 0;
					//}  /*EndIf*/
			
					// Pointer always dword
					iv_Data[1] = (IntPtr)AutoItApi.DllStructGetData(v_Buffer, 1, "dword");
			
				}  /*EndIf*/
		
			}
	
			iv_Data[0] = iv_Address;

			return iv_Data[1];
		}

/*;=================================================================================================
; Function:			_MemoryPointerWrite ($iv_Address, $ah_Handle, $av_Offset, $v_Data[, $sv_Type])
; Description:		Reads a chain of pointers and writes the data to the destination address.
; Parameter(s):		$iv_Address - The static memory address you want to start at. It must be in
;								  hex format (0x00000000).
;					$ah_Handle - An array containing the Dll handle and the handle of the open
;								 process as returned by _MemoryOpen().
;					$av_Offset - An array of offsets for the pointers.  Each pointer must have an
;								 offset.  If there is no offset for a pointer, enter 0 for that
;								 array dimension.
;					$v_Data - The data to be written.
;					$sv_Type - (optional) The "Type" of data you intend to write at the destination
;								 address.  This is set to 'dword'(32bit(4byte) signed integer) by
;								 default.  See the help file for DllStructCreate for all types.
; Requirement(s):	The $ah_Handle returned from _MemoryOpen.
; Return Value(s):	On Success - Returns the destination address.
;					On Failure - Returns 0.
;					@Error - 0 = No error.
;							 1 = $av_Offset is not an array.
;							 2 = Invalid $ah_Handle.
;							 3 = Failed to read from the specified process.
;							 4 = $sv_Type is not a string.
;							 5 = $sv_Type is an unknown data type.
;							 6 = Failed to allocate the memory needed for the DllStructure.
;							 7 = Error allocating memory for $sv_Type.
;							 8 = $v_Data is not in the proper format to be used with the
;								 "Type" selected for $sv_Type, or it is out of range.
;							 9 = Failed to write to the specified process.
; Author(s):		Nomad
; Note(s):			Data written is in Decimal format, unless a 'char' type is selected.
;					Set $av_Offset like this:
;					$av_Offset[0] = NULL (not used, doesn't matter what's entered)
;					$av_Offset[1] = Offset for pointer 1 (all offsets must be in Decimal)
;					$av_Offset[2] = Offset for pointer 2
;					etc...
;					(The number of array dimensions determines the number of pointers)
;=================================================================================================*/
//public void _MemoryPointerWrite ($iv_Address, $ah_Handle, $av_Offset, $v_Data, $sv_Type = 'dword')
//{	
//	If IsArray($av_Offset) {  /*Then*/
//		If IsArray($ah_Handle) {  /*Then*/
//			Local $iv_PointerCount = UBound($av_Offset) - 1
//		Else
//			SetError(2)
//			Return 0
//		}  /*EndIf*/
//	Else
//		SetError(1)
//		Return 0
//	}  /*EndIf*/
	
//	Local $iv_StructData, $i
//	Local $v_Buffer = DllStructCreate('dword')

//	For $i = 0 to $iv_PointerCount
//		If $i = $iv_PointerCount {  /*Then*/
//			$v_Buffer = DllStructCreate($sv_Type)
//			If @Error {  /*Then*/
//				SetError(@Error + 3)
//				Return 0
//			}  /*EndIf*/
			
//			DllStructSetData($v_Buffer, 1, $v_Data)
//			If @Error {  /*Then*/
//				SetError(8)
//				Return 0
//			}  /*EndIf*/
			
//			$iv_Address = '0x' & hex($iv_StructData + $av_Offset[$i])
//			DllCall($ah_Handle[0], 'int', 'WriteProcessMemory', 'int', $ah_Handle[1], 'int', $iv_Address, 'ptr', DllStructGetPtr($v_Buffer), 'int', DllStructGetSize($v_Buffer), 'int', '')
//			If @Error {  /*Then*/
//				SetError(9)
//				Return 0
//			Else
//				Return $iv_Address
//			}  /*EndIf*/
//		ElseIf $i = 0 {  /*Then*/
//			DllCall($ah_Handle[0], 'int', 'ReadProcessMemory', 'int', $ah_Handle[1], 'int', $iv_Address, 'ptr', DllStructGetPtr($v_Buffer), 'int', DllStructGetSize($v_Buffer), 'int', '')
//			If @Error {  /*Then*/
//				SetError(3)
//				Return 0
//			}  /*EndIf*/
			
//			$iv_StructData = DllStructGetData($v_Buffer, 1)
			
//		Else
//			$iv_Address = '0x' & hex($iv_StructData + $av_Offset[$i])
//			DllCall($ah_Handle[0], 'int', 'ReadProcessMemory', 'int', $ah_Handle[1], 'int', $iv_Address, 'ptr', DllStructGetPtr($v_Buffer), 'int', DllStructGetSize($v_Buffer), 'int', '')
//			If @Error {  /*Then*/
//				SetError(3)
//				Return 0
//			}  /*EndIf*/
			
//			$iv_StructData = DllStructGetData($v_Buffer, 1)
			
//		}  /*EndIf*/
//	Next

//}
	}
}
