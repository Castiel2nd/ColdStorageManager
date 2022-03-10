// source: https://stackoverflow.com/questions/55132739/how-do-i-enumerate-nvme-m2-drives-an-get-their-temperature-in-c

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ColdStorageManager.Annotations;

namespace ColdStorageManager
{
	public static class NVMeQuery
	{
		[DllImport("NVMeQuery.dll", CallingConvention = CallingConvention.StdCall)]
		internal static extern IntPtr New(InteropBase.AssetCallback callback);
		[DllImport("NVMeQuery.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void GetSerialNumber(IntPtr p, int physicalDriveId, byte[] serialNumber);
	}

	public class NVMeQueries : InteropBase
	{
		public NVMeQueries()
		{
			_ptr = NVMeQuery.New(_onAssetErrorMessageChanged);
		}

		public string GetSerialNumber(int physicalDriveId)
		{
			byte[] serialNumber = new byte[21];
			NVMeQuery.GetSerialNumber(_ptr, physicalDriveId, serialNumber);
			return System.Text.Encoding.ASCII.GetString(serialNumber);
		}

		// public ulong GetTemp() => GetTemp(@"\\.\PhysicalDrive2");
		//
		// public ulong GetTemp(string drivePath)
		// {
		// 	var strPtr = Marshal.StringToHGlobalAuto(drivePath);
		// 	var result = NVMeQuery.GetTemp(_ptr, strPtr);
		// 	Marshal.FreeHGlobal(strPtr);
		// 	return result;
		// }
	}

	public class InteropBase : INotifyPropertyChanged
	{
		protected IntPtr _ptr;
		protected readonly AssetCallback _onAssetErrorMessageChanged;

		public delegate void AssetCallback(IntPtr strPtr);

		public List<string> LogMessages { get; private set; } = new List<string>();

		public InteropBase()
		{
			_onAssetErrorMessageChanged = LogUpdater;
		}

		private unsafe void LogUpdater(IntPtr strPtr)
		{
			var LastLogMessage = new string((char*)strPtr);
			Console.WriteLine(LastLogMessage);
			LogMessages.Add(LastLogMessage);
			OnPropertyChanged(nameof(LogMessages));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
