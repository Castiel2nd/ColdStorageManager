using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ColdStorageManager
{
	//class for collecting all the unmanaged function references and interfacing methods
	internal sealed class Win32
	{
		private const uint SHGFI_ICON = 0x100;
		private const uint SHGFI_LARGEICON = 0x0;
		private const uint SHGFI_SMALLICON = 0x1;
		private const int FILE_ATTRIBUTE_NORMAL = 0x80;
		private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
		[StructLayout(LayoutKind.Sequential)]
		public struct SHFILEINFO
		{
			public IntPtr hIcon;
			public IntPtr iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		};

		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("shell32.dll")]
		private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
			ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

		public static SHFILEINFO shinfo = new SHFILEINFO();

		public static Icon Extract(string path)
		{
			IntPtr hIcon = SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);
			//The icon is returned in the hIcon member of the shinfo struct
			var icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
			DestroyIcon(shinfo.hIcon);
			return icon;
		}

		public static ImageSource ToImageSource(Icon icon)
		{
			ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
				icon.Handle,
				Int32Rect.Empty,
				BitmapSizeOptions.FromEmptyOptions());

			return imageSource;
		}

		[DllImport("User32.dll")]
		public static extern int DestroyIcon(IntPtr hIcon);

		// Hide console window by invoking the method once MainWindow is shown
		public static void HideConsoleWindow()
		{
			IntPtr hWnd = Process.GetCurrentProcess().MainWindowHandle;

			if (hWnd != IntPtr.Zero)
			{
				ShowWindow(hWnd, 0); // 0 = SW_HIDE
			}
		}
	}
}
