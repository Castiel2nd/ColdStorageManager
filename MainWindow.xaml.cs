using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Windows.Interop;

namespace ColdStorageManager
{
	//static class for global variables
	public static class Globals
	{
		public static string version = "v0.1";
		public static MainWindow mainWindow = null;
		public static string configFileName = "CSM.config";
		public static Configuration configFile;
		public static KeyValueConfigurationCollection settings;
		public static List<PhysicalDrive> physicalDrives;
		public static Partition selectedPartition;
		public static WpfObservableRangeCollection<CSMFileSystemEntry> fileDialogEntryTree;
		public static List<CSMFileSystemEntry> fsList;
		public static TextBlock statusBarTb;


		private static string[] sizes =
		{
			" B", " KB", " MB", " GB", " TB", " PB", " EB"
		};
		public static string GetFormattedSize(in ulong inSize)
		{
			double size = (double)inSize;
			int i;
			for (i = 0; size >= 1024F; i++)
			{
				//Console.WriteLine(size);
				size /= 1024;
			}

			return string.Format("{0:0.00}", size) + sizes[i];
		}

		public static List<CSMFileSystemEntry> GetFileSystemEntries(in string path, in bool getIcon = true)
		{
			List<CSMFileSystemEntry> fileSystemEntries = new List<CSMFileSystemEntry>();

			string[] dirs = null;
			try
			{
				dirs = Directory.GetDirectories(path);
			}
			catch (UnauthorizedAccessException e)
			{
				//TODO
			}
			catch (Exception e)
			{
				//TODO
			}

			if (dirs == null)
			{
				return fileSystemEntries;
			}

			foreach (string dir in dirs)
			{
				fileSystemEntries.Add(new CSMDirectory(dir, getIcon));
			}

			foreach (string file in Directory.GetFiles(path))
			{
				fileSystemEntries.Add(new CSMFile(file, getIcon));
			}

			return fileSystemEntries;
		}
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>

	public partial class MainWindow : Window
	{

		private Dictionary<string, string> defaultSettings = new Dictionary<string, string>()
		{
			{ "language","en-US" }
		};
		public MainWindow()
		{
			LoadSettings();
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			InitializeComponent();
			Title = "Cold Storage Manager " + Globals.version;
			trvDrives.ItemsSource = Globals.physicalDrives;
			Globals.fileDialogEntryTree = new WpfObservableRangeCollection<CSMFileSystemEntry>();
			Globals.statusBarTb = statusbarText;
			statusbarText.Text = Application.Current.Resources["ready"].ToString();
			Globals.mainWindow = this;

		}

		private void LoadSettings()
		{
			var configFileMap = new ExeConfigurationFileMap();
			configFileMap.ExeConfigFilename = Globals.configFileName;

			try
			{
				var configFile = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
				Globals.configFile = configFile;
				//Console.WriteLine(configFile.FilePath);

				var settings = configFile.AppSettings.Settings;
				Globals.settings = settings;
				foreach(KeyValuePair<string, string> setting in defaultSettings)
				{
					if(settings[setting.Key] == null)
					{
						settings.Add(setting.Key, setting.Value);
					}
				}
				configFile.Save(ConfigurationSaveMode.Modified);
				ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
			}
			catch (ConfigurationErrorsException e)
			{
				Console.WriteLine("Configuration error: " + e.BareMessage);
			}

			//if set language is not the default, load it
			if(Globals.settings["language"].Value != "en-US")
			{
				LoadLanguage("en-US");
			}
		}
		
		public void LoadLanguage(string prev)
		{
			string lang = Globals.settings["language"].Value;

			var resourceDictionaryToLoad = new ResourceDictionary();
			resourceDictionaryToLoad.Source =
				new Uri("Language/"+lang+".xaml",
						UriKind.RelativeOrAbsolute);

			var current = Application.Current.Resources.MergedDictionaries.FirstOrDefault(
					m => m.Source.OriginalString.EndsWith(prev+".xaml"));

			if (current != null)
			{
				Application.Current.Resources.MergedDictionaries.Remove(current);
			}

			Application.Current.Resources.MergedDictionaries.Add(resourceDictionaryToLoad);
		}

		private void OptionsMenu_Click(object sender, RoutedEventArgs e)
		{
			Options.GetInstance().Show();
		}

		private void PartitionSelected(object sender, RoutedEventArgs e)
		{
			TreeView trvSender = sender as TreeView;
			//Console.WriteLine(trvSender.SelectedItem);
			//Console.WriteLine(selected.GetType() + " " + typeof(Partition));
			if (trvSender.SelectedItem is Partition selected)
			{
				Globals.selectedPartition = selected;
				driveTxtBx.Text = selected.Parent.Model + ((bool)snChkBx.IsChecked ? " [" + selected.Parent.SerialNumber + "]" : "");
				Globals.fileDialogEntryTree.Clear();
				Globals.fileDialogEntryTree.AddRange(Globals.GetFileSystemEntries(selected.Letter+"\\"));
				trvFileDialog.ItemsSource = Globals.fileDialogEntryTree;
			}
		}

		private void ConvertFSObservListToList()
		{
			Globals.fsList = Globals.fileDialogEntryTree.ToList();
			foreach (var entry in Globals.fsList)
			{
				if (entry is CSMDirectory dir)
				{
					dir.ConvertChildrenToListPropagate();
				}
			}
		}

		//Fully expands FSList to include every entry on the partition
		private void ExpandFSList()
		{
			foreach (var entry in Globals.fsList)
			{
				if (entry is CSMDirectory dir)
				{
					if (!dir.IsExpanded)
					{
						dir.ExpandChildrenListPropagate();
					}
				}
			}
		}

		private void TrvFileDialog_OnExpanded(object sender, RoutedEventArgs e)
		{
			TreeViewItem item = e.OriginalSource as TreeViewItem;
			if (item.DataContext != null && item.DataContext.GetType() == typeof(CSMDirectory))
			{
				CSMDirectory dir = item.DataContext as CSMDirectory;
				//Console.WriteLine(dir);
				if (dir.Children.Count == 1)
				{
					dir.Children.Clear();
					var list = Globals.GetFileSystemEntries(dir.Path);
					dir.Children.AddRange(list);
					dir.IsExpanded = true;
				}
				e.Handled = true;
			}
		}

		private void Capture_Click(object sender, RoutedEventArgs e)
		{
			ConvertFSObservListToList();
			ExpandFSList();
			CFSHandler.WriteCFS("test", driveTxtBx.Text, nicknameTxtBx.Text);
			statusbarText.Text = "Successfully captured " + Globals.selectedPartition.Letter + " [" +
			                     Globals.selectedPartition.Label + "]";
		}

		private void SnChkBx_OnClick(object sender, RoutedEventArgs e)
		{
			if (Globals.selectedPartition != null)
			{
				CheckBox cb = sender as CheckBox;
				driveTxtBx.Text = Globals.selectedPartition.Parent.Model + ((bool)snChkBx.IsChecked ? " [" + Globals.selectedPartition.Parent.SerialNumber + "]" : "");

			}
		}
	}

	//class for collecting all the unmanaged function references and interfacing methods
	internal sealed class Win32
	{
		private const uint SHGFI_ICON = 0x100;
		private const uint SHGFI_LARGEICON = 0x0;
		private const uint SHGFI_SMALLICON = 0x1;
		private const int FILE_ATTRIBUTE_NORMAL = 0x80;
		private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
		[StructLayout(LayoutKind.Sequential)]
		private struct SHFILEINFO
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

		public static Icon Extract(string fileName)
		{
			var shinfo = new SHFILEINFO();

			IntPtr hIcon = SHGetFileInfo(fileName, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);
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
