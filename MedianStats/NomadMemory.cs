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
		enum ProcessAccessFlags : uint
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
		static extern IntPtr OpenProcess(
			 ProcessAccessFlags processAccess,
			 bool bInheritHandle,
			 int processId
		);

		static IntPtr _WinAPI_OpenProcess(Process proc, ProcessAccessFlags flags)
		{
			 return OpenProcess(flags, false, proc.Id);
		}


		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool ReadProcessMemory(
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
		static extern bool WriteProcessMemory(
			IntPtr hProcess,
			IntPtr lpBaseAddress,
			byte[] lpBuffer,
			Int32 nSize,
			out IntPtr lpNumberOfBytesWritten);



		/**
		 * Description:		Opens a process and enables all possible access rights to the process.  The
		 *					Process ID of the process is used to specify which process to open.  You must
		 *					call this function before calling _MemoryClose(), _MemoryRead(), or _MemoryWrite().
		 *					
		 * Parameter(s):	iv_Pid - The Process ID of the program you want to open.
		 *					iv_DesiredAccess - (optional) Set to 0x1F0FFF by default, which enables all
		 *										possible access rights to the process specified by the
		 *										Process ID.
		 *					if_InheritHandle - (optional) If this value is TRUE, all processes created by
		 *										this process will inherit the access handle.  Set to TRUE
		 *										(1) by default.  Set to 0 if you want it to be FALSE.
		 * Requirement(s):	A valid process ID.
		 * 
		 * Return Value(s): On Success - Returns an array containing the Dll handle and an open handle to
		 *								 the specified process.
		 *					On Failure - Returns 0
		 */
		public static IntPtr MemoryOpen(int iv_Pid, int iv_DesiredAccess = 0x1F0FFF, bool if_InheritHandle = true)
		{
			var av_OpenProcess = OpenProcess((ProcessAccessFlags)iv_DesiredAccess, if_InheritHandle, iv_Pid/*, true*/);
	
			IntPtr ah_Handle = av_OpenProcess;
	
			return ah_Handle;
		}

		/**
		 * Description:		Reads the value located in the memory address specified.
		 * 
		 * Parameter(s):	iv_Address - The memory address you want to read from. It must be in hex
		 *								  format (0x00000000).
		 *					ah_Handle - An array containing the Dll handle and the handle of the open
		 *								 process as returned by _MemoryOpen().
		 *					sv_Type - (optional) The "Type" of value you intend to read.  This is set to
		 *								'dword'(32bit(4byte) signed integer) by default.  See the help file
		 *								for DllStructCreate for all types.
		 *								An example: If you want to read a word that is 15 characters in
		 *								length, you would use 'char[16]'.
		 * Requirement(s):	The ah_Handle returned from _MemoryOpen.
		 * Return Value(s):	On Success - Returns the value located at the specified address.
		 *					On Failure - Returns 0
		 */
		public static int MemoryRead(IntPtr iv_Address, IntPtr ah_Handle, string sv_Type = "dword")
		{
			var v_Buffer = AutoItApi.DllStructCreate(sv_Type);
	
			int numberBytesRead = 0;
			ReadProcessMemory(ah_Handle, iv_Address, v_Buffer, AutoItApi.DllStructGetSize(v_Buffer), out numberBytesRead);
	
			var v_Value = AutoItApi.DllStructGetData(v_Buffer, 1, sv_Type);
			return v_Value;
		}

		/// <summary>
		/// NOTE: Not original! von mir estellt
		/// </summary>
		public static byte[] MemoryRead(IntPtr iv_Address, IntPtr ah_Handle, byte[] v_Buffer)
		{
			int numberBytesRead = 0;
			ReadProcessMemory(ah_Handle, iv_Address, v_Buffer, AutoItApi.DllStructGetSize(v_Buffer), out numberBytesRead);

			return v_Buffer;
		}

		/**
		 * Description:		Writes data to the specified memory address.
		 * 
		 * Parameter(s):	iv_Address - The memory address you want to write to.  It must be in hex
		 *								  format (0x00000000).
		 *					ah_Handle - An array containing the Dll handle and the handle of the open
		 *								 process as returned by _MemoryOpen().
		 *					v_Data - The data to be written.
		 *					sv_Type - (optional) The "Type" of value you intend to write.  This is set to
		 *								'dword'(32bit(4byte) signed integer) by default.  See the help file
		 *								for DllStructCreate for all types.
		 *								An example: If you want to write a word that is 15 characters in
		 *								length, you would use 'char[16]'.
		 * Requirement(s):	The ah_Handle returned from _MemoryOpen.
		 * Return Value(s):	On Success - Returns 1
		 *					On Failure - Returns 0
		 */
		public static void MemoryWrite(IntPtr iv_Address, IntPtr ah_Handle, string v_Data, string sv_Type = "dword")
		{
			MemoryWrite(iv_Address, ah_Handle, StringToByteArray(v_Data), sv_Type);
		}

		public static void MemoryWrite(IntPtr iv_Address, IntPtr ah_Handle, byte[] v_Data, string sv_Type = "dword")
		{
			var v_Buffer = AutoItApi.DllStructCreate(sv_Type);

			AutoItApi.DllStructSetData(v_Buffer, 1, v_Data);

			IntPtr lpNumberOfBytesWritten;
			WriteProcessMemory(ah_Handle, iv_Address, v_Buffer, v_Buffer.Length, out lpNumberOfBytesWritten);
		}

		public static uint MemoryWriteHexString(IntPtr iv_Address, IntPtr ah_Handle, string v_Data)
		{
			return MemoryWrite(iv_Address, ah_Handle, HexStringToByteArray(v_Data));
		}

		public static void MemoryWrite(IntPtr iv_Address, IntPtr ah_Handle, string v_Data)
		{
			MemoryWrite(iv_Address, ah_Handle, StringToByteArray(v_Data));
		}

		public static uint MemoryWrite(IntPtr iv_Address, IntPtr ah_Handle, byte[] v_Data)
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

		/**
		 * Description:		Closes the process handle opened by using _MemoryOpen().
		 * 
		 * Parameter(s):	ah_Handle - An array containing the Dll handle and the handle of the open
		 *								 process as returned by _MemoryOpen().
		 * Requirement(s):	The ah_Handle returned from _MemoryOpen.
		 * Return Value(s):	On Success - Returns 1
		 *					On Failure - Returns 0
		 */
		public static void MemoryClose(IntPtr ah_Handle)
		{
			CloseHandle(ah_Handle);
		}

		/**
		 * @brief		    Reads a chain of pointers and returns an array containing the destination
		 *					address and the data at the address.
		 * Parameter(s):		iv_Address - The static memory address you want to start at. It must be in
		 *								  hex format (0x00000000).
		 *					ah_Handle - An array containing the Dll handle and the handle of the open
		 *								 process as returned by _MemoryOpen().
		 *					av_Offset - An array of offsets for the pointers.  Each pointer must have an
		 *								 offset.  If there is no offset for a pointer, enter 0 for that
		 *								 array dimension.
		 *					sv_Type - (optional) The "Type" of data you intend to read at the destination
		 *								 address.  This is set to 'dword'(32bit(4byte) signed integer) by
		 *								 default.  See the help file for DllStructCreate for all types.
		 * Requirement(s):	The ah_Handle returned from _MemoryOpen.
		 * Return Value(s):	On Success - Returns an array containing the destination address and the value
		 *								 located at the address.
		 *					On Failure - Returns 0
		 * Note(s):			Values returned are in Decimal format, unless a 'char' type is selected.
		 *					Set av_Offset like this:
		 *					av_Offset[0] = NULL (not used)
		 *					av_Offset[1] = Offset for pointer 1 (all offsets must be in Decimal)
		 *					av_Offset[2] = Offset for pointer 2
		 *					etc...
		 *					(The number of array dimensions determines the number of pointers)
		 */
		public static IntPtr MemoryPointerRead(IntPtr iv_Address, IntPtr ah_Handle, int[] av_Offset, string sv_Type = "dword")
		{
			var iv_PointerCount = av_Offset.Length - 1;

			IntPtr[] iv_Data = new IntPtr[2];
			var v_Buffer = AutoItApi.DllStructCreate("dword");

			int numberBytesRead = 0;
			for (int i = 0; i <= iv_PointerCount; i++) {
		
				if (i == iv_PointerCount) {
					// Last Pointer

					v_Buffer = AutoItApi.DllStructCreate(sv_Type);
			
					iv_Address = iv_Data[1] + av_Offset[i];
					ReadProcessMemory(ah_Handle, iv_Address, v_Buffer, AutoItApi.DllStructGetSize(v_Buffer), out numberBytesRead);


					iv_Data[1] = (IntPtr)AutoItApi.DllStructGetData(v_Buffer, 1, sv_Type);
			
				} else if (i == 0) {
					// First Pointer

					ReadProcessMemory(ah_Handle, iv_Address, v_Buffer, AutoItApi.DllStructGetSize(v_Buffer), out numberBytesRead);

					// Pointer always dword
					iv_Data[1] = (IntPtr)AutoItApi.DllStructGetData(v_Buffer, 1, "dword");
			
				} else {
					iv_Address = iv_Data[1] + av_Offset[i];
					ReadProcessMemory(ah_Handle, iv_Address, v_Buffer, AutoItApi.DllStructGetSize(v_Buffer), out numberBytesRead);
			
					// Pointer always dword
					iv_Data[1] = (IntPtr)AutoItApi.DllStructGetData(v_Buffer, 1, "dword");
				}
			}
	
			iv_Data[0] = iv_Address;

			return iv_Data[1];
		}
	}
}
