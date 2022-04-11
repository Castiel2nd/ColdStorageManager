using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ColdStorageManager.DBManagers;
using ColdStorageManager.Models;

namespace ColdStorageManager
{
	//static class for global variables
	public static class Globals
	{
		//app.cs
		public static char[] volumeGuidDelimiterArray = new char[] { '{', '}' };
		public const string LineSeparator = "\n";
		public const string ConfigNameOfLocalDS = "local";

		public const string version = "v0.4";
		public static string startupWindowLocation;
		public static double[] startupWindowLocationCoords;
		public static MainWindow mainWindow;

		public static ObservableCollection<DataSource> dataSources = new ObservableCollection<DataSource>();
		public static DataSource selectedDataSource = null;
		public static ObservableCollection<DbConnectionProfile> dbConnectionProfiles = new ObservableCollection<DbConnectionProfile>();

		public static List<PhysicalDrive> physicalDrives;
		public static TreeView capturesTrv;
		public static WpfObservableRangeCollection<CaptureBase> capturesDisplay = new WpfObservableRangeCollection<CaptureBase>();

		public static double remoteCaptureCacheTTL = 60; //how long the remote captures can be cached for
		public static bool copyFromListEventsEnabled = true;
		public static bool copyToListEventsEnabled = true;

		public static Partition selectedPartition;

		public static WpfObservableRangeCollection<CSMFileSystemEntry> fileDialogEntryTree = new WpfObservableRangeCollection<CSMFileSystemEntry>();
		public static List<CSMFileSystemEntry> fsList;

		public static WpfObservableRangeCollection<SearchResultControl> filesFound = new WpfObservableRangeCollection<SearchResultControl>();
		public static List<int> fileResultsColumnsOrder;
		public static List<double> fileResultsColumnWidths;
		public static WpfObservableRangeCollection<SearchResultControl> dirsFound = new WpfObservableRangeCollection<SearchResultControl>();
		public static List<int> dirResultsColumnsOrder;
		public static List<double> dirResultsColumnWidths;


		public const ushort numBlobTypes = 5;

		//Log stuff
		public static LogHolder logHolder = new LogHolder();
		public static LogPanel dbLogPanel;
		public static LogPanel logPanel;

		// options
		public const string configFileName = "CSM.config";
		public static Configuration configFile;
		public static KeyValueConfigurationCollection settings;
		public static bool ExceptionPathsEnable;
		public static bool ExceptionFileTypesCaptureEnable;
		public static bool ExceptionFileTypesSearchEnable;
		public static ObservableCollection<string> exceptionPathsOC;
		public static List<string> exceptionFileTypesCaptureList;
		public static List<string> exceptionFileTypesSearchList;

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

		//bitmasks for capture properties
		public const ushort SIZE = 0b1;
		public const ushort CREATION_TIME = 0b10;
		public const ushort LAST_ACCESS_TIME = 0b100;
		public const ushort LAST_MODIFICATION_TIME = 0b1000;

		public static ushort ToProperties(bool size, bool createTime, bool accessTime, bool modTime)
		{
			Collection<int> rnw = new Collection<int>();
			ushort captureProperties = 0;
			if (size)
				captureProperties ^= Globals.SIZE;
			if (createTime)
				captureProperties ^= Globals.CREATION_TIME;
			if (accessTime)
				captureProperties ^= Globals.LAST_ACCESS_TIME;
			if (modTime)
				captureProperties ^= Globals.LAST_MODIFICATION_TIME;
			return captureProperties;
		}

		public static (bool size, bool createTime, bool lastAccessTime, bool lastModTime) DecodeCaptureProperties(ushort captureProperties)
		{
			return (
					((captureProperties & Globals.SIZE) == Globals.SIZE),
					((captureProperties & Globals.CREATION_TIME) == Globals.CREATION_TIME),
					((captureProperties & Globals.LAST_ACCESS_TIME) == Globals.LAST_ACCESS_TIME),
					((captureProperties & Globals.LAST_MODIFICATION_TIME) == Globals.LAST_MODIFICATION_TIME)
			);
		}

		public static string GetSectionSetting(string section, string key)
		{
			return (configFile.Sections.Get(section) as AppSettingsSection).Settings[key]?.Value;
		}

		public static AppSettingsSection GetSection(string sectionName)
		{
			return configFile.Sections.Get(sectionName) as AppSettingsSection;
		}

		public static void SetSectionSetting(string section, string key, string value)
		{
			(configFile.Sections.Get(section) as AppSettingsSection).Settings[key].Value = value;
		}

		public static void SetNewSectionSetting(string section, string key, string value)
		{
			(configFile.Sections.Get(section) as AppSettingsSection).Settings.Add(key, value);
		}

		public static string GetLocalizedString(string key)
		{
			return Application.Current.Resources[key].ToString();
		}

		public static List<CSMFileSystemEntry> GetFileSystemEntries(in string path, in CSMDirectory parent = null, in bool getIcon = true)
		{
			List<CSMFileSystemEntry> fileSystemEntries = new List<CSMFileSystemEntry>();

			string[] dirs = null;
			try
			{
				dirs = Directory.GetDirectories(path);
			}
			catch (UnauthorizedAccessException e)
			{
				if (parent != null)
				{
					parent.IsUnaccessible = true;
				}
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
				fileSystemEntries.Add(new CSMDirectory(dir, parent, getIcon));
			}

			foreach (string file in Directory.GetFiles(path))
			{
				fileSystemEntries.Add(new CSMFile(file, parent, getIcon));
			}

			return fileSystemEntries;
		}

		public static TreeViewItem FindTviFromObjectRecursive(ItemsControl ic, object o)
		{
			//Search for the object model in first level children (recursively)
			TreeViewItem tvi = ic.ItemContainerGenerator.ContainerFromItem(o) as TreeViewItem;
			if (tvi != null) return tvi;
			//Loop through user object models
			foreach (object i in ic.Items)
			{
				//Get the TreeViewItem associated with the iterated object model
				TreeViewItem tvi2 = ic.ItemContainerGenerator.ContainerFromItem(i) as TreeViewItem;
				tvi = FindTviFromObjectRecursive(tvi2, o);
				if (tvi != null) return tvi;
			}
			return null;
		}

		public static IEnumerable<string> GetFileTypesFromString(string filetypes)
		{
			return filetypes.Split(',').Select(type => "." + type.Trim());
		}

		public static void MoveItemInList<T>(int oldIndex, int newIndex, List<T> list)
		{
			T item = list[oldIndex];
			list.RemoveAt(oldIndex);
			list.Insert(newIndex, item);
		}

		public static System.Windows.Point GetPositionRelativeTo(Control controlToGetThePositionOf, Control relativeToThis)
		{
			return controlToGetThePositionOf.TransformToAncestor(relativeToThis).Transform(new System.Windows.Point(0, 0));
		}

		public static void DisplayInfoMessageBox(string windowTitle, string localizedDesc)
		{
			DisplayInfoMessageBoxWithOwner(windowTitle, localizedDesc, mainWindow);
		}

		public static void DisplayInfoMessageBoxWithOwner(string windowTitle, string localizedDesc, Window owner)
		{
			MessageBox.Show(owner, localizedDesc,
				windowTitle, MessageBoxButton.OK, MessageBoxImage.Information,
				MessageBoxResult.OK);
		}

		public static void DisplayErrorMessageBoxWithDescription(string windowTitle, string localizedDesc, string exceptionMessage)
		{
			DisplayErrorMessageBoxWithDescriptionWithOwner(windowTitle, localizedDesc, exceptionMessage, mainWindow);
		}

		public static void DisplayErrorMessageBoxWithDescriptionWithOwner(string windowTitle, string localizedDesc, string exceptionMessage, Window owner)
		{
			MessageBox.Show(owner, localizedDesc + LineSeparator + LineSeparator
												 + GetLocalizedString("description") + LineSeparator + LineSeparator
												 + exceptionMessage,
				windowTitle, MessageBoxButton.OK, MessageBoxImage.Error,
				MessageBoxResult.OK);
		}

		public static void SaveMySQLDbConnectionProfilesToMem()
		{
			GetSection("mysqlConnectionProfiles").Settings.Clear();
			foreach (var mySqlDbConnectionProfile in dbConnectionProfiles)
			{
				SetNewSectionSetting("mysqlConnectionProfiles", mySqlDbConnectionProfile.ProfileName, mySqlDbConnectionProfile.ToString());
			}
		}

		public static DbConnectionProfile GetMySqlDbConnectionProfileByName(string profileName)
		{
			foreach (var profile in dbConnectionProfiles)
			{
				if (profile.ProfileName.Equals(profileName))
				{
					return profile;
				}
			}

			return null;
		}

		public static DataSource GetDataSourceByName(string name)
		{
			foreach (var dataSource in dataSources)
			{
				if (dataSource.Name.Equals(name))
				{
					return dataSource;
				}
			}

			return null;
		}

		public static DataSource DeleteDataSourceByName(string name)
		{
			foreach (var dataSource in dataSources)
			{
				if (dataSource.Name.Equals(name))
				{
					return dataSource;
				}
			}

			return null;
		}

		public static void WriteConfigFileToDisk()
		{
			configFile.Save(ConfigurationSaveMode.Modified);
		}
	}
}
