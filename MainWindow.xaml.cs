
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using ColdStorageManager.Annotations;
using ColdStorageManager.DBManagers;
using ColdStorageManager.Models;
using MySql.Data.MySqlClient;
using static ColdStorageManager.Globals;
using static ColdStorageManager.Logger;
using Point = System.Drawing.Point;

namespace ColdStorageManager
{
	//static class for global variables
	public static class Globals
	{
		//app.cs
		public static char[] volumeGuidDelimiterArray = new char[] { '{', '}'};

		public const string LineSeparator = "\n";

		public const string version = "v0.3";
		public static string startupWindowLocation;
		public static double[] startupWindowLocationCoords;
		public static MainWindow mainWindow;

		public static IDbManager selectedDbManager = null;
		public static MySQLDbManager activeMySQLDbManager;
		public static SQLiteDbManager localSqLiteDbManager;
		public static List<MySQLDbConnectionProfile> mySqlDbConnectionProfiles = new List<MySQLDbConnectionProfile>();

		public static List<PhysicalDrive> physicalDrives;
		public static TreeView capturesTrv;
		public static WpfObservableRangeCollection<CaptureBase> capturesDisplay = new WpfObservableRangeCollection<CaptureBase>();
		public static List<Capture> capturesList = new List<Capture>();

		public static double remoteCaptureCacheTTL = 60; //how long the remote captures can be cached for
		//these lists are used for caching and to facilitate copying between connections
		public static List<CaptureBase> localCapturesDisplayCache;
		public static List<CaptureBase> remoteCapturesDisplayCache;
		public static List<Capture> localCapturesCache;
		public static List<Capture> remoteCapturesCache;
		public static DateTime remoteCapturesCacheDateTime;
		public static string remoteCapturesCacheSource; //where are the remote captures from
		public static List<string> allConnectionNames = new List<string>();
		public static ObservableCollection<string> copyFromList = new ObservableCollection<string>();
		public static ObservableCollection<string> copyToList = new ObservableCollection<string>();
		public static ObservableCollection<string> captureConnList = new ObservableCollection<string>();
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
			foreach (var mySqlDbConnectionProfile in mySqlDbConnectionProfiles)
			{
				SetNewSectionSetting("mysqlConnectionProfiles", mySqlDbConnectionProfile.ProfileName, mySqlDbConnectionProfile.ToString());
			}
		}

		public static MySQLDbConnectionProfile GetMySqlDbConnectionProfileByName(string profileName)
		{
			foreach (var profile in mySqlDbConnectionProfiles)
			{
				if (profile.ProfileName.Equals(profileName))
				{
					return profile;
				}	
			}

			return null;
		}

		public static IDbManager GetDbManagerFromConnectionName(string connName)
		{
			if (connName.Equals(GetLocalizedString("local")))
			{
				return localSqLiteDbManager;
			}
			else if(activeMySQLDbManager != null && activeMySQLDbManager.Profile.ProfileName.Equals(connName))
			{
				return activeMySQLDbManager;
			}
			else
			{
				return new MySQLDbManager(GetMySqlDbConnectionProfileByName(connName));
			}
		}

		//return wether the cache could be used
		public static bool UseLocalCapturesCache()
		{
			if (localCapturesDisplayCache != null && localCapturesDisplayCache.Count > 0)
			{
				capturesDisplay.Clear();
				capturesDisplay.AddRange(localCapturesDisplayCache);
				capturesList.Clear();
				capturesList.AddRange(localCapturesCache);
				return true;
			}

			return false;
		}

		public static void InvalidateRemoteCapturesCache()
		{
			remoteCapturesCacheSource = "";
		}

		public static void WriteConfigFileToDisk()
		{
			configFile.Save(ConfigurationSaveMode.Modified);
		}
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>

	public partial class MainWindow : Window
	{

		private Dictionary<string, string> defaultSettings = new Dictionary<string, string>()
		{
			{ "language","en-US" },
			{ "startupWindowState","normal" },
			{ "startupWindowLocation","remember" },
			{ "startupWindowLocationCoords","0,0" },
		};

		private Dictionary<string, Dictionary<string, string>> defaultSectionSettings =
			new Dictionary<string, Dictionary<string, string>>()
			{
				{
					"searchSettings", new Dictionary<string, string>()
					{
						{ "selectedCaptureConnection", "local" },
						{ "searchQuery", "" },
						{ "sizeEnabled", "False" },
						{ "sizeRelCmbx_selectedIndex", "0" },
						{ "sizeSlider_value", "0" },
						{ "sizeCmbx_selectedIndex", "0" },
						{ "creationTimeEnable", "False" },
						{ "lastAccessEnable", "False" },
						{ "lastModTimeEnable", "False" },
						{ "createTimeRelCmbx_selectedIndex", "0" },
						{ "createTimeDP", "" },
						{ "accessTimeRelCmbx_selectedIndex", "0" },
						{ "accessTimeDP", "" },
						{ "lastModTimeRelCmbx_selectedIndex", "0" },
						{ "lastModTimeDP", "" },
						// 0=filename, 1=path, 2=size, 3=creation_time, 4=access_time, 5=mod_time
						{ "fileResultsColumnsOrder", "0,1,2,3,4,5" },
						{ "dirResultsColumnsOrder", "0,1,2,3,4,5" },
						{ "fileResultsColumnWidths", "70,100,60,70,70,70" },
						{ "dirResultsColumnWidths", "70,100,60,70,70,70" },
					}
				},
				{
					"captureSettings", new Dictionary<string, string>()
					{
						{ "capPropSizeCb", "False" },
						{ "capPropCreateTimeCb", "False" },
						{ "capPropLastAccessCb", "False" },
						{ "capPropLastModCb", "False" },
					}
				},
				{
					"exceptions", new Dictionary<string, string>()
					{
						{ "exceptionPathsEnableCb", "True" },
						{ "exceptionsList", "\\Windows\n" +
											"\\System Volume Information\n" +
											"\\$RECYCLE.BIN\n" +
											"\\$Recycle.Bin" },
						{ "exceptionFileTypesCaptureEnableCb", "True" },
						{ "exceptionFileTypesCaptureTxtBx", "tmp" },
						{ "exceptionFileTypesSearchEnableCb", "False" },
						{ "exceptionFileTypesSearchTxtBx", "" },
					}
				},
				{
					"mysqlConnectionProfiles", new Dictionary<string, string>()
					{
						
					}
				}
			};

		private bool captureConnSelectionChangedEnabled = false;

		public MainWindow()
		{
			LoadSettings();
			InitVars();

			InitializeComponent();
			
			InitVarsAfterUI();
			LoadSettingsAfterUI();

			RefreshCapturesWithFallback("local", 0, true);
		}

		private void InitVars()
		{
			Title = $"Cold Storage Manager {version}";

			Globals.mainWindow = this;
		}

		private void InitVarsAfterUI()
		{
			Globals.capturesTrv = trvCaptures;

			//setup logpanels
			Globals.dbLogPanel = dbLogPanel;
			dbLogPanel.SetBorder(true, true, false);
			dbLogPanel.SetRefresher(RefreshDbLogPopupOffsets);
			Binding dbLogBinding = new Binding(nameof(logHolder.databaseLog));
			dbLogBinding.Source = logHolder;
			dbLogBinding.Mode = BindingMode.OneWay;
			dbLogPanel.BindDisplayData(dbLogBinding);
			Globals.logPanel = logPanel;
			logPanel.SetBorder(false, true, true);
			logPanel.SetRefresher(RefreshLogPopupOffsets);
			Binding logBinding = new Binding(nameof(logHolder.log));
			logBinding.Source = logHolder;
			logBinding.Mode = BindingMode.OneWay;
			logPanel.BindDisplayData(logBinding);

			//setup status displays
			Binding statusBinding = new Binding(nameof(logHolder.Status));
			statusBinding.Source = logHolder;
			statusBinding.Mode = BindingMode.OneWay;
			statusBarTb.SetBinding(TextBlock.TextProperty, statusBinding);
			Binding dbStatusBinding = new Binding(nameof(logHolder.DbStatus));
			dbStatusBinding.Source = logHolder;
			dbStatusBinding.Mode = BindingMode.OneWay;
			dbStatusBarTb.SetBinding(TextBlock.TextProperty, dbStatusBinding);

			trvDrives.ItemsSource = Globals.physicalDrives;
			trvFileDialog.ItemsSource = Globals.fileDialogEntryTree;
			fileListView.ItemsSource = Globals.filesFound;
			dirListView.ItemsSource = Globals.dirsFound;
			trvCaptures.ItemsSource = Globals.capturesDisplay;

			Logger.statusBarTb = statusBarTb;
			Logger.dbStatusBarTb = dbStatusBarTb;
			Logger.dbStatusEllipse = dbStatusEllipse;

			Globals.localSqLiteDbManager = new SQLiteDbManager();

			LogAction(GetLocalizedString("ready"), "Init complete");
		}

		//save settings on window close
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
		private void MainWindow_OnClosed(object? sender, EventArgs e)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
		{
			// general
			if (Globals.startupWindowLocation.Equals("remember"))
			{
				Globals.startupWindowLocationCoords[0] = Left;
				Globals.startupWindowLocationCoords[1] = Top;
				Globals.settings["startupWindowLocationCoords"].Value =
					string.Join(',', Globals.startupWindowLocationCoords);
				switch (WindowState)
				{
					case WindowState.Maximized:
						Globals.settings["startupWindowState"].Value = "maximized";
						break;
					case WindowState.Normal:
						Globals.settings["startupWindowState"].Value = "normal";
						break;
				}
			}
			

			//searchSettings
			Globals.SetSectionSetting("searchSettings", "searchQuery", searchTxtBox.Text);
			Globals.SetSectionSetting("searchSettings", "sizeEnabled", sizeEnable.IsChecked.ToString());
			Globals.SetSectionSetting("searchSettings", "sizeRelCmbx_selectedIndex", sizeRelCmbx.SelectedIndex.ToString());
			Globals.SetSectionSetting("searchSettings", "sizeSlider_value", sizeSlider.Value.ToString());
			Globals.SetSectionSetting("searchSettings", "sizeCmbx_selectedIndex", sizeCmbx.SelectedIndex.ToString());
			Globals.SetSectionSetting("searchSettings", "creationTimeEnable", creationTimeEnable.IsChecked.ToString());
			Globals.SetSectionSetting("searchSettings", "createTimeRelCmbx_selectedIndex", createTimeRelCmbx.SelectedIndex.ToString());
			Globals.SetSectionSetting("searchSettings", "createTimeDP", createTimeDP.Text);
			Globals.SetSectionSetting("searchSettings", "lastAccessEnable", lastAccessEnable.IsChecked.ToString());
			Globals.SetSectionSetting("searchSettings", "accessTimeRelCmbx_selectedIndex", accessTimeRelCmbx.SelectedIndex.ToString());
			Globals.SetSectionSetting("searchSettings", "accessTimeDP", accessTimeDP.Text);
			Globals.SetSectionSetting("searchSettings", "lastModTimeEnable", lastModTimeEnable.IsChecked.ToString());
			Globals.SetSectionSetting("searchSettings", "lastModTimeRelCmbx_selectedIndex", lastModTimeRelCmbx.SelectedIndex.ToString());
			Globals.SetSectionSetting("searchSettings", "lastModTimeDP", lastModTimeDP.Text);
			Globals.SetSectionSetting("searchSettings", "fileResultsColumnsOrder", string.Join(',', Globals.fileResultsColumnsOrder));
			Globals.SetSectionSetting("searchSettings", "fileResultsColumnWidths", string.Join(',', Globals.fileResultsColumnWidths));
			Globals.SetSectionSetting("searchSettings", "dirResultsColumnsOrder", string.Join(',', Globals.dirResultsColumnsOrder));
			Globals.SetSectionSetting("searchSettings", "dirResultsColumnWidths", string.Join(',', Globals.dirResultsColumnWidths));

			//captureSettings
			Globals.SetSectionSetting("captureSettings", "capPropSizeCb", capPropSizeCb.IsChecked.ToString());
			Globals.SetSectionSetting("captureSettings", "capPropCreateTimeCb", capPropCreateTimeCb.IsChecked.ToString());
			Globals.SetSectionSetting("captureSettings", "capPropLastAccessCb", capPropLastAccessCb.IsChecked.ToString());
			Globals.SetSectionSetting("captureSettings", "capPropLastModCb", capPropLastModCb.IsChecked.ToString());

			//mysqlConnectionProfiles
			SaveMySQLDbConnectionProfilesToMem();
		}

		private void LoadSettings()
		{
			var configFileMap = new ExeConfigurationFileMap();
			configFileMap.ExeConfigFilename = Globals.configFileName;
			AppSettingsSection section;
			try
			{
				var configFile = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
				Globals.configFile = configFile;
				//configFile.Sections.Add("general", new AppSettingsSection());
				//AppSettingsSection sec = configFile.Sections.Get("general") as AppSettingsSection;
				//sec.Settings.Add("hello", "test");

				var settings = configFile.AppSettings.Settings;
				Globals.settings = settings;
				//load default settings
				foreach(KeyValuePair<string, string> setting in defaultSettings)
				{
					if(settings[setting.Key] == null)
					{
						settings.Add(setting.Key, setting.Value);
					}
				}
				//load default section settings
				foreach (var sectionInfo in defaultSectionSettings)
				{
					if (configFile.Sections.Get(sectionInfo.Key) == null)
					{
						configFile.Sections.Add(sectionInfo.Key, new AppSettingsSection());
					}

					foreach (var setting in sectionInfo.Value)
					{
						section = configFile.Sections.Get(sectionInfo.Key) as AppSettingsSection;
						if (section.Settings[setting.Key] == null)
						{
							section.Settings.Add(setting.Key, setting.Value);
						}
					}
				}
				configFile.Save(ConfigurationSaveMode.Modified);
				ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
			}
			catch (ConfigurationErrorsException e)
			{
				Console.WriteLine("Configuration error: " + e.BareMessage);
			}

			//loading settings
			//if set language is not the default, load it
			if(Globals.settings["language"].Value != "en-US")
			{
				LoadLanguage("en-US");
			}

			//defaultSettings other than language
			Globals.startupWindowLocation = Globals.settings["startupWindowLocation"].Value;
			Globals.startupWindowLocationCoords =
				Globals.settings["startupWindowLocationCoords"].Value.Split(',').Select(double.Parse).ToArray();
			if ((Globals.startupWindowLocationCoords.All(d => d == 0)) || Globals.startupWindowLocation.Equals("center"))
			{
				WindowStartupLocation = WindowStartupLocation.CenterScreen;
			}else if (Globals.startupWindowLocation.Equals("remember"))
			{
				Left = Globals.startupWindowLocationCoords[0];
				Top = Globals.startupWindowLocationCoords[1];
				if (Globals.settings["startupWindowState"].Value.Equals("maximized"))
				{
					WindowState = WindowState.Maximized;
				}
			}

			//exceptions
			section = Globals.configFile.Sections.Get("exceptions") as AppSettingsSection;
			Globals.ExceptionPathsEnable = bool.Parse(section.Settings["exceptionPathsEnableCb"].Value);
			Globals.ExceptionFileTypesCaptureEnable = bool.Parse(section.Settings["exceptionFileTypesCaptureEnableCb"].Value);
			Globals.ExceptionFileTypesSearchEnable = bool.Parse(section.Settings["exceptionFileTypesSearchEnableCb"].Value);
			Globals.exceptionPathsOC = new ObservableCollection<string>(section.Settings["exceptionsList"].Value.Split("\n"));
			Globals.exceptionFileTypesCaptureList = new List<string>(Globals.GetFileTypesFromString(section.Settings["exceptionFileTypesCaptureTxtBx"].Value));
			Globals.exceptionFileTypesSearchList = new List<string>(Globals.GetFileTypesFromString(section.Settings["exceptionFileTypesSearchTxtBx"].Value));

			// searchSettings
			section = Globals.configFile.Sections.Get("searchSettings") as AppSettingsSection;
			Globals.fileResultsColumnsOrder =
				new List<int>(section.Settings["fileResultsColumnsOrder"].Value.Split(",").Select(int.Parse));
			Globals.fileResultsColumnWidths =
				new List<double>(section.Settings["fileResultsColumnWidths"].Value.Split(",").Select(double.Parse));
			Globals.dirResultsColumnsOrder =
				new List<int>(section.Settings["dirResultsColumnsOrder"].Value.Split(",").Select(int.Parse));
			Globals.dirResultsColumnWidths =
				new List<double>(section.Settings["dirResultsColumnWidths"].Value.Split(",").Select(double.Parse));

			//mysqlConnectionProfiles
			section = Globals.configFile.Sections.Get("mysqlConnectionProfiles") as AppSettingsSection;
			foreach (var profileName in section.Settings.AllKeys)
			{
				mySqlDbConnectionProfiles.Add(new MySQLDbConnectionProfile(profileName, section.Settings[profileName].Value));
			}

		}

		private void LoadSettingsAfterUI()
		{
			//searchSettings
			AppSettingsSection section = Globals.configFile.Sections.Get("searchSettings") as AppSettingsSection;
			searchTxtBox.Text = section.Settings["searchQuery"].Value;
			sizeEnable.IsChecked = bool.Parse(section.Settings["sizeEnabled"].Value);
			if(sizeEnable.IsChecked == false)
				SizeEnable_OnUnchecked(null, null);
			creationTimeEnable.IsChecked = bool.Parse(section.Settings["creationTimeEnable"].Value);
			if (creationTimeEnable.IsChecked == false)
				CreationTimeEnable_OnUnchecked(null, null);
			lastAccessEnable.IsChecked = bool.Parse(section.Settings["lastAccessEnable"].Value);
			if (lastAccessEnable.IsChecked == false)
				LastAccessEnable_OnUnchecked(null, null);
			lastModTimeEnable.IsChecked = bool.Parse(section.Settings["lastModTimeEnable"].Value);
			if (lastModTimeEnable.IsChecked == false)
				LastModTimeEnable_OnUnchecked(null, null);
			sizeRelCmbx.SelectedIndex = Int32.Parse(section.Settings["sizeRelCmbx_selectedIndex"].Value);
			sizeCmbx.SelectedIndex = Int32.Parse(section.Settings["sizeCmbx_selectedIndex"].Value);
			sizeSlider.Value = Int32.Parse(section.Settings["sizeSlider_value"].Value);
			createTimeRelCmbx.SelectedIndex = Int32.Parse(section.Settings["createTimeRelCmbx_selectedIndex"].Value);
			createTimeDP.Text = section.Settings["createTimeDP"].Value;
			accessTimeRelCmbx.SelectedIndex = Int32.Parse(section.Settings["accessTimeRelCmbx_selectedIndex"].Value);
			accessTimeDP.Text = section.Settings["accessTimeDP"].Value;
			lastModTimeRelCmbx.SelectedIndex = Int32.Parse(section.Settings["lastModTimeRelCmbx_selectedIndex"].Value);
			lastModTimeDP.Text = section.Settings["lastModTimeDP"].Value;
			RefreshCaptureConnectionsCmbx(true);
			SetSelectedDbManager();

			//copy all cmbx options setup
			copyFromCmbx.ItemsSource = copyFromList;
			copyToCmbx.ItemsSource = copyToList;

			//captureSettings
			LoadCaptureSettings();
		}

		public void LoadCaptureSettings()
		{
			AppSettingsSection section = Globals.configFile.Sections.Get("captureSettings") as AppSettingsSection;
			capPropSizeCb.IsChecked = bool.Parse(section.Settings["capPropSizeCb"].Value);
			capPropCreateTimeCb.IsChecked = bool.Parse(section.Settings["capPropCreateTimeCb"].Value);
			capPropLastAccessCb.IsChecked = bool.Parse(section.Settings["capPropLastAccessCb"].Value);
			capPropLastModCb.IsChecked = bool.Parse(section.Settings["capPropLastModCb"].Value);
		}

		public void RemoveDisplayConnectionName(string connName)
		{
			copyFromListEventsEnabled = false;
			copyToListEventsEnabled = false;

			allConnectionNames.Remove(connName);
			copyFromList.Remove(connName);
			copyToList.Remove(connName);
			captureConnList.Remove(connName);

			copyFromListEventsEnabled = true;
			copyToListEventsEnabled = true;
		}

		public void AddDisplayConnectionName(string connName)
		{
			allConnectionNames.Add(connName);
			copyFromList.Add(connName);
			copyToList.Add(connName);
			captureConnList.Add(connName);
		}

		private void RefreshCaptureConnectionsCmbx(bool init = false)
		{
			captureConnSelectionChangedEnabled = false;

			int selectedIndex = captureConnCmbx.SelectedIndex;
			string selectedName = "";

			// if we're in the initialization phase, the previous selected name should come from the configuration
			if (init)
			{
				selectedName = GetSectionSetting("searchSettings", "selectedCaptureConnection");
			}

			if (selectedIndex != -1)
			{
				selectedName = captureConnCmbx.SelectedItem.ToString();
			}

			captureConnCmbx.ItemsSource = captureConnList;
			AddDisplayConnectionName(GetLocalizedString("local"));

			foreach (var mySqlDbConnectionProfile in mySqlDbConnectionProfiles)
			{
				AddDisplayConnectionName(mySqlDbConnectionProfile.ProfileName);
				if (selectedName.Equals(mySqlDbConnectionProfile.ProfileName))
				{
					captureConnCmbx.SelectedItem = selectedName;
				}
			}

			if (captureConnCmbx.SelectedIndex == -1)
			{
				captureConnCmbx.SelectedIndex = 0;
			}

			captureConnSelectionChangedEnabled = true;
		}

		// sets the 'selectedDbManager' and potentially 'activeMySQLDbManager' as indicated by the 'selectedCaptureConnection' setting in the local configuration
		// assumes 'localSqLiteDbManager' has been initialized
		// only the variable is being set, no connection is initiated
		private void SetSelectedDbManager()
		{
			string connectionName = GetSectionSetting("searchSettings", "selectedCaptureConnection");
			if (connectionName.Equals("local"))
			{
				selectedDbManager = localSqLiteDbManager;
			}
			else
			{
				// if the old MySQLDbManager can be used, do so
				if (activeMySQLDbManager != null && activeMySQLDbManager.Profile.ProfileName.Equals(connectionName))
				{
					selectedDbManager = activeMySQLDbManager;
					return;
				}
				else if(selectedDbManager is MySQLDbManager) //if changing from mysql to another mysql connection, first close the old one.
				{
					selectedDbManager.CloseConnection();
					selectedDbManager = null;
					activeMySQLDbManager = null;
				}

				var dbMgr = GetDbManagerFromConnectionName(connectionName);

				//fallback to SQLite if profile not found due to possible configuration corruption
				if (dbMgr == null)
				{
					selectedDbManager = localSqLiteDbManager;
					captureConnSelectionChangedEnabled = false;
					captureConnCmbx.SelectedIndex = 0;
					captureConnSelectionChangedEnabled = true;

					LogAction(GetLocalizedString("db_manager_not_found"), $"Failed to find DbManager: {connectionName}. Possible memory corruption.", ERROR);
				}
				else
				{
					selectedDbManager = dbMgr;
					activeMySQLDbManager = (MySQLDbManager)dbMgr;
				}
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
				driveTxtBx.Text = selected.Parent.Model;
				driveSnTxtBx.Text = selected.Parent.SerialNumber;
				Globals.fileDialogEntryTree.Clear();
				Globals.fileDialogEntryTree.AddRange(Globals.GetFileSystemEntries(selected.Letter+"\\"));

				if (selected.IsCaptured)
				{
					var capProps = Globals.DecodeCaptureProperties(selected.Capture.capture_properties);
					capPropSizeCb.IsChecked = capProps.size;
					capPropCreateTimeCb.IsChecked = capProps.createTime;
					capPropLastAccessCb.IsChecked = capProps.lastAccessTime;
					capPropLastModCb.IsChecked = capProps.lastModTime;
					nicknameTxtBx.Text = selected.Capture.drive_nickname;
				}
				else
				{
					LoadCaptureSettings();
					nicknameTxtBx.Text = "";
				}
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
			if (item.DataContext is CSMDirectory dir)
			{
				//Console.WriteLine(dir);
				if (!dir.IsExpanded)
				{
					dir.Children.Clear();
					var list = Globals.GetFileSystemEntries(dir.Path, dir);
					dir.Children.AddRange(list);
					dir.IsExpanded = true;
				}
				e.Handled = true;
			}
		}

		private void Capture_Click(object sender, RoutedEventArgs e)
		{
			ushort captureProperties = Globals.ToProperties(capPropSizeCb.IsChecked == true, capPropCreateTimeCb.IsChecked == true,
				capPropLastAccessCb.IsChecked == true, capPropLastModCb.IsChecked == true);
			ConvertFSObservListToList();
			ExpandFSList();
			var cfsBytes = CFSHandler.GetCFSBytes(Globals.selectedPartition.Parent.Model, nicknameTxtBx.Text,
				capPropSizeCb.IsChecked == true,
				capPropCreateTimeCb.IsChecked == true,
				capPropLastAccessCb.IsChecked == true,
				capPropLastModCb.IsChecked == true);

			Capture newCapture = new Capture(Globals.selectedPartition.Parent.Model,
				Globals.selectedPartition.Parent.SerialNumber,
				Globals.selectedPartition.Parent.isNVMe,
				Globals.selectedPartition.Parent.NVMeSerialNumberDetectionFail,
				Globals.selectedPartition.Parent.TotalSpace,
				nicknameTxtBx.Text,
				Globals.selectedPartition.Label,
				Globals.selectedPartition.Index,
				Globals.selectedPartition.TotalSpace,
				Globals.selectedPartition.FreeSpace,
				Globals.selectedPartition.VolumeGUID,
				captureProperties,
				DateTime.Now.ToString(),
				cfsBytes.lines,
				cfsBytes.files,
				cfsBytes.dirs,
				cfsBytes.capture,
				cfsBytes.sizes,
				cfsBytes.creation_times,
				cfsBytes.last_access_times,
				cfsBytes.last_mod_times
			);

			if (Globals.selectedPartition.IsCaptured)
			{
				newCapture.id = Globals.selectedPartition.Capture.id;
				Globals.selectedDbManager.UpdateCaptureById(newCapture);
			}
			else
			{
				Globals.selectedDbManager.SaveCapture(newCapture);
			}


			//CFSHandler.WriteCFS("test", driveTxtBx.Text, nicknameTxtBx.Text);
			LogAction(GetLocalizedString("capture_successful") + Globals.selectedPartition.Letter + " [" +
			                 Globals.selectedPartition.Label + "]", $"Successfully captured {Globals.selectedPartition.Letter} [{Globals.selectedPartition.Label}]");

			if (selectedDbManager is MySQLDbManager)
			{
				InvalidateRemoteCapturesCache();
			}

			RefreshCaptures();
		}

		private List<CaptureBase> GetCapturesDisplayListFromCapturesList(List<Capture> captures)
		{
			List<CaptureBase> capturesDisplayList = new List<CaptureBase>();

			if (captures.Count > 0)
			{
				searchButton.IsEnabled = true;
				searchButton.ToolTip = null;
				foreach (var capture in captures)
				{
					capture.SetViews();
					CFSHandler.PrepareCapture(capture);
					bool found = false;
					foreach (var capturePhDisk in capturesDisplayList)
					{
						if (capturePhDisk.drive_nickname == capture.drive_nickname &&
						    capturePhDisk.drive_sn == capture.drive_sn)
						{
							found = true;
							((CapturePhDisk)capturePhDisk).captures.Add(capture);
							break;
						}
					}

					if (!found)
					{
						CapturePhDisk phDisk =
							new CapturePhDisk(capture.drive_model, capture.drive_sn, capture.drive_size,
								capture.drive_nickname);

						phDisk.captures.Add(capture);
						capturesDisplayList.Add(phDisk);
					}
				}

				//sort captures per drive per partition number
				foreach (var capturePhDisk in capturesDisplayList)
				{
					((CapturePhDisk)capturePhDisk).captures.Sort((Capture cap1, Capture cap2) =>
						cap1.partition_number.CompareTo(cap2.partition_number));
				}
			}
			else
			{
				capturesDisplayList.Add(new CapturePlaceholder(Globals.GetLocalizedString("no_captures_placeholder")));
				searchButton.IsEnabled = false;
				searchButton.ToolTip = Globals.GetLocalizedString("no_captures_tooltip");
			}

			return capturesDisplayList;
		}

		//wrapper for RefreshCaptures that checks it's return value and falls back on a specified connection if necessary
		private void RefreshCapturesWithFallback(string configNameOfFallbackConnection = "local", int indexOfFallbackConnection = 0, bool repeatRefresh = false)
		{
			if (!RefreshCaptures())
			{
				//if fallback conn is the same as the one already attempted and is not the local, then fall back to the local
				if (selectedDbManager == GetDbManagerFromConnectionName(configNameOfFallbackConnection) && !(selectedDbManager is SQLiteDbManager))
				{
					SetSectionSetting("searchSettings", "selectedCaptureConnection", "local");
					SetSelectedDbManager();

					captureConnSelectionChangedEnabled = false;
					captureConnCmbx.SelectedIndex = 0;
					captureConnSelectionChangedEnabled = true;

					if (!RefreshCaptures()) // if the local fails too, display error
					{
						captureConnSelectionChangedEnabled = false;
						captureConnCmbx.SelectedIndex = -1;
						captureConnSelectionChangedEnabled = true;
						capturesDisplay.Add(new CapturePlaceholder(GetLocalizedString("could_not_connect_placeholder")));
					}
					return;
				}
				else if (selectedDbManager == GetDbManagerFromConnectionName(configNameOfFallbackConnection)) //if fallback conn is the same as the one already attempted and is local, display error
				{
					captureConnSelectionChangedEnabled = false;
					captureConnCmbx.SelectedIndex = -1;
					captureConnSelectionChangedEnabled = true;
					capturesDisplay.Add(new CapturePlaceholder(GetLocalizedString("could_not_connect_placeholder")));
					return;
				}

				SetSectionSetting("searchSettings", "selectedCaptureConnection", configNameOfFallbackConnection);
				SetSelectedDbManager();

				captureConnSelectionChangedEnabled = false;
				captureConnCmbx.SelectedIndex = indexOfFallbackConnection;
				captureConnSelectionChangedEnabled = true;

				if (repeatRefresh && !RefreshCaptures())
				{
					if (indexOfFallbackConnection == 0)
					{
						captureConnSelectionChangedEnabled = false;
						captureConnCmbx.SelectedIndex = -1;
						captureConnSelectionChangedEnabled = true;
						capturesDisplay.Add(new CapturePlaceholder(GetLocalizedString("could_not_connect_placeholder")));
					}
					else
					{
						SetSectionSetting("searchSettings", "selectedCaptureConnection", "local");
						SetSelectedDbManager();

						captureConnSelectionChangedEnabled = false;
						captureConnCmbx.SelectedIndex = 0;
						captureConnSelectionChangedEnabled = true;

						if (!RefreshCaptures())
						{
							captureConnSelectionChangedEnabled = false;
							captureConnCmbx.SelectedIndex = -1;
							captureConnSelectionChangedEnabled = true;
							capturesDisplay.Add(new CapturePlaceholder(GetLocalizedString("could_not_connect_placeholder")));
						}
					}
				}
				else //if repeated retry isn't allowed, display error msg after 2 failed attempts
				{
					captureConnSelectionChangedEnabled = false;
					captureConnCmbx.SelectedIndex = -1;
					captureConnSelectionChangedEnabled = true;
					capturesDisplay.Add(new CapturePlaceholder(GetLocalizedString("could_not_connect_placeholder")));
				}
			}
		}

		//returns whether it could get the captures
		private bool RefreshCaptures()
		{
			//connect if not already connected
			if (selectedDbManager is MySQLDbManager mySqlDbManager)
			{
				if (!mySqlDbManager.IsConnected)
				{
					if (!mySqlDbManager.ConnectAndCreateTableIfNotFound())
					{
						//do nothing if connection failed
						return false;
					}
				}

				//if the cache is alive, use it
				if (mySqlDbManager.Profile.ProfileName.Equals(remoteCapturesCacheSource) &&
				    (DateTime.Now - remoteCapturesCacheDateTime).TotalSeconds < remoteCaptureCacheTTL)
				{
					capturesDisplay.Clear();
					capturesDisplay.AddRange(remoteCapturesDisplayCache);
					capturesList.Clear();
					capturesList.AddRange(remoteCapturesCache);
					return true;
				}
			}

			List<Capture> captures = Globals.selectedDbManager.GetCaptures();
			List<CaptureBase> capturesDisplayList = GetCapturesDisplayListFromCapturesList(captures);
			if (selectedDbManager is SQLiteDbManager)
			{
				localCapturesDisplayCache = capturesDisplayList;
				localCapturesCache = captures;
			}
			else
			{
				remoteCapturesDisplayCache = capturesDisplayList;
				remoteCapturesCache = captures;
				remoteCapturesCacheSource = ((MySQLDbManager)selectedDbManager).Profile.ProfileName;
				remoteCapturesCacheDateTime = DateTime.Now;
			}

			MatchCapturesDisplayListToPartitions(capturesDisplayList);
			capturesDisplay.Clear();
			capturesDisplay.AddRange(capturesDisplayList);
			capturesList.Clear();
			capturesList.AddRange(captures);

			return true;
		}

		private void MatchCapturesDisplayListToPartitions(List<CaptureBase> capturesDisplayList)
		{
			//resetting linked captures
			foreach (var physicalDrive in Globals.physicalDrives)
			{
				foreach (var partition in physicalDrive.Partitions)
				{
					partition.IsCaptured = false;
					partition.Capture = null;
				}
			}

			//matching captures to partitions
			foreach (var genericCapture in capturesDisplayList)
			{
				if (genericCapture is CapturePhDisk capturePhDisk)
				{
					foreach (var capture in capturePhDisk.captures)
					{
						foreach (var physicalDrive in Globals.physicalDrives)
						{
							foreach (var partition in physicalDrive.Partitions)
							{
								if (capture.volume_guid.Equals(partition.VolumeGUID) && capture.drive_model.Equals(
									    partition.Parent.Model)
								    && capture.drive_sn.Equals(partition.Parent
									    .SerialNumber))
								{
									// Console.WriteLine("Found capture for: " + partition.Label);
									partition.IsCaptured = true;
									partition.Capture = capture;
								}
							}
						}
					}
				}
			}
		}

		private void Search(string stringToSearch)
		{
			if (stringToSearch.Length > 2)
			{
				Globals.filesFound.Clear();
				Globals.dirsFound.Clear();
				List<SearchResultControl> filesFound = new List<SearchResultControl>();
				List<SearchResultControl> dirsFound = new List<SearchResultControl>();

				//preparing detailed search information
				ushort searchProperties = Globals.ToProperties(
					(sizeEnable.IsChecked == true) && (sizeRelCmbx.SelectedIndex != -1) &&
					(sizeCmbx.SelectedIndex != -1),
					(creationTimeEnable.IsChecked == true) && (createTimeRelCmbx.SelectedIndex != -1) && (createTimeDP.SelectedDate != null),
					(lastAccessEnable.IsChecked == true) && (accessTimeRelCmbx.SelectedIndex != -1) && (accessTimeDP.SelectedDate != null),
					 (lastModTimeEnable.IsChecked == true) && (lastModTimeRelCmbx.SelectedIndex != -1) && (lastModTimeDP.SelectedDate != null));
				long sizeToSearch = (long)sizeSlider.Value;
				for (int i = sizeCmbx.SelectedIndex; i > 0; i--)
					sizeToSearch *= 1024;
				DateTime createTimeToSearch = createTimeDP.SelectedDate ?? default;
				DateTime accessTimeToSearch = accessTimeDP.SelectedDate ?? default;
				DateTime lastModTimeToSearch = lastModTimeDP.SelectedDate ?? default;
				
				foreach (var captureGl in Globals.capturesDisplay)
				{
					if (captureGl is CapturePhDisk capturePhDisk)
					{
						foreach (var capture in capturePhDisk.captures)
						{
							var res = CFSHandler.Search(capture.capture, capture.capture_properties, 
								searchProperties,
								stringToSearch,
								sizeRelCmbx.SelectedIndex, sizeToSearch,
								createTimeRelCmbx.SelectedIndex, createTimeToSearch,
								accessTimeRelCmbx.SelectedIndex, accessTimeToSearch,
								lastModTimeRelCmbx.SelectedIndex, lastModTimeToSearch,
								capture.sizes_prepared, capture.creation_times_prepared, capture.last_access_times_prepared, capture.last_mod_times_prepared);

							if (res.files.Count != 0)
							{
								filesFound.Add(new SearchResultControl(new SearchResultCaptureView(capturePhDisk, capture, res.files), true));
							}
							if (res.dirs.Count != 0)
							{
								dirsFound.Add(new SearchResultControl(new SearchResultCaptureView(capturePhDisk, capture, res.dirs), false));
							}
						}
					}
				}

				if (filesFound.Count != 0)
				{
					Globals.filesFound.AddRange(filesFound);
				}
				if (dirsFound.Count != 0)
				{
					Globals.dirsFound.AddRange(dirsFound);
				}
			}
		}

		private void SearchButton_Click(object sender, RoutedEventArgs e)
		{
			Search(searchTxtBox.Text);
		}

		private void SizeEnable_OnChecked(object sender, RoutedEventArgs e)
		{
			sizeRelCmbx.IsEnabled = true;
			sizeSlider.IsEnabled = true;
			sizeSldTxt.IsEnabled = true;
			sizeCmbx.IsEnabled = true;
		}

		private void SizeEnable_OnUnchecked(object sender, RoutedEventArgs e)
		{
			sizeRelCmbx.IsEnabled = false;
			sizeSlider.IsEnabled = false;
			sizeSldTxt.IsEnabled = false;
			sizeCmbx.IsEnabled = false;
		}

		private void LastAccessEnable_OnChecked(object sender, RoutedEventArgs e)
		{
			accessTimeRelCmbx.IsEnabled = true;
			accessTimeDP.IsEnabled = true;
		}

		private void LastAccessEnable_OnUnchecked(object sender, RoutedEventArgs e)
		{
			accessTimeRelCmbx.IsEnabled = false;
			accessTimeDP.IsEnabled= false;
		}

		private void CreationTimeEnable_OnChecked(object sender, RoutedEventArgs e)
		{
			createTimeRelCmbx.IsEnabled = true;
			createTimeDP.IsEnabled= true;
		}

		private void CreationTimeEnable_OnUnchecked(object sender, RoutedEventArgs e)
		{
			createTimeRelCmbx.IsEnabled = false;
			createTimeDP.IsEnabled=false;
		}

		private void LastModTimeEnable_OnChecked(object sender, RoutedEventArgs e)
		{
			lastModTimeRelCmbx.IsEnabled = true;
			lastModTimeDP.IsEnabled = true;
		}

		private void LastModTimeEnable_OnUnchecked(object sender, RoutedEventArgs e)
		{
			lastModTimeRelCmbx.IsEnabled = false;
			lastModTimeDP.IsEnabled = false;
		}

		private void SearchResultsListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			((SearchResultControl)(e.AddedItems[0]))?.SelectCorrespondingCapture();
		}

		private void MySQLMenuItem_Click(object sender, RoutedEventArgs e)
		{
			new MySQLConnectWindow(this);
		}

		private void CaptureConnCmbx_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (captureConnSelectionChangedEnabled)
			{
				string previousSelectedConnection = GetSectionSetting("searchSettings", "selectedCaptureConnection");
				if (captureConnCmbx.SelectedIndex == 0)
				{
					SetSectionSetting("searchSettings", "selectedCaptureConnection", "local");
					SetSelectedDbManager();

					if (UseLocalCapturesCache())
					{
						return;
					}
				}
				else if (captureConnCmbx.SelectedIndex == -1)
				{
					captureConnCmbx.SelectedIndex = 0;
					return;
				}
				else
				{
					SetSectionSetting("searchSettings", "selectedCaptureConnection", e.AddedItems[0].ToString());
					SetSelectedDbManager();
				}

				

				//if refreshing failed for some reason, fall back to previous connection
				if (e.RemovedItems.Count > 0)
				{
					RefreshCapturesWithFallback(previousSelectedConnection, captureConnCmbx.Items.IndexOf(e.RemovedItems[0]));
				}
				else // if there was nothing selected, try local as fallback
				{
					RefreshCapturesWithFallback("local", 0);
				}	
			}
		}

		public void RefreshDbLogPopupOffsets()
		{
			System.Windows.Point dbStatusBtnPosition = GetPositionRelativeTo(dbStatusBtn, mainWindow);
			System.Windows.Point mainStatusBarPosition = GetPositionRelativeTo(mainStatusBar, mainWindow);

			dbLogPopup.HorizontalOffset = (mainWindow.ActualWidth - dbStatusBtnPosition.X) - dbLogPanel.Width - 17;
			dbLogPopup.VerticalOffset = mainStatusBarPosition.Y - dbStatusBtnPosition.Y - 1;
		}

		public void RefreshLogPopupOffsets()
		{
			System.Windows.Point statusBtnPosition = GetPositionRelativeTo(statusBtn, mainWindow);
			System.Windows.Point mainStatusBarPosition = GetPositionRelativeTo(mainStatusBar, mainWindow);

			logPopup.HorizontalOffset =  - statusBtnPosition.X;
			logPopup.VerticalOffset = mainStatusBarPosition.Y - statusBtnPosition.Y - 1;
		}

		private void DbStatusBtn_OnClick(object sender, RoutedEventArgs e)
		{
			RefreshDbLogPopupOffsets();
			dbLogPopup.IsOpen = true;
		}

		private void StatusBtn_OnClick(object sender, RoutedEventArgs e)
		{
			RefreshLogPopupOffsets();
			logPopup.IsOpen = true;
		}

		private void CopyAllBtn_OnClick(object sender, RoutedEventArgs e)
		{
			if (copyFromCmbx.SelectedValue != null && copyToCmbx.SelectedValue != null)
			{
				if (!copyFromCmbx.SelectedValue.ToString().Equals(copyToCmbx.SelectedValue.ToString()))
				{
					IDbManager from, to;

					from = GetDbManagerFromConnectionName(copyFromCmbx.SelectedValue.ToString());
					to = GetDbManagerFromConnectionName(copyToCmbx.SelectedValue.ToString());

					if (!from.IsConnected)
					{
						if (!from.ConnectAndCreateTableIfNotFound())
						{
							return;
						}
					}

					if (!to.IsConnected)
					{
						Console.WriteLine("to");
						if (!to.ConnectAndCreateTableIfNotFound())
						{
							return;
						}
					}

					//both connected
					var captures = from.GetCaptures();
					foreach (var capture in captures)
					{
						to.SaveCapture(capture);
					}

					if (to == selectedDbManager)
					{
						if (selectedDbManager is MySQLDbManager)
						{
							InvalidateRemoteCapturesCache();
						}

						RefreshCapturesWithFallback("local", 0, true);
					}
				}
				else
				{
					LogActionSameMsg("Copy From and To cannot be the same!!", WARNING);
				}
			}
			else
			{
				LogActionSameMsg("You must select a connection for From and To as well!!", WARNING);
			}
		}

		private void CopyFromCmbx_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (copyFromListEventsEnabled)
			{
				if (e.RemovedItems.Count > 0 && e.AddedItems[0].ToString().Equals(copyToCmbx.SelectedValue))
				{
					copyToListEventsEnabled = false;
					copyToCmbx.SelectedValue = e.RemovedItems[0].ToString();
					copyToListEventsEnabled = true;
				}else if (e.AddedItems[0].ToString().Equals(copyToCmbx.SelectedValue))
				{
					copyToListEventsEnabled = false;
					copyToCmbx.SelectedIndex = -1;
					copyToListEventsEnabled = true;
				}
			}
		}

		private void CopyToCmbx_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (copyToListEventsEnabled)
			{
				if (e.RemovedItems.Count > 0 && e.AddedItems[0].ToString().Equals(copyFromCmbx.SelectedValue))
				{
					copyFromListEventsEnabled = false;
					copyFromCmbx.SelectedValue = e.RemovedItems[0].ToString();
					copyFromListEventsEnabled = true;
				}else if (e.AddedItems[0].ToString().Equals(copyFromCmbx.SelectedValue))
				{
					copyFromListEventsEnabled = false;
					copyFromCmbx.SelectedIndex = -1;
					copyFromListEventsEnabled = true;
				}
			}
		}

		private void DeleteBtn_Click(object sender, RoutedEventArgs e)
		{
			if (trvCaptures.SelectedItem is Capture capture)
			{
				Globals.selectedDbManager.DeleteCapture(capture);
				
				if (selectedDbManager is MySQLDbManager)
				{
					InvalidateRemoteCapturesCache();
				}

				RefreshCaptures();
			}
		}

		private void DelAllBtn_OnClick(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show(this,
				    $"{GetLocalizedString("del_all_warning")} {captureConnCmbx.SelectedValue.ToString()}.{LineSeparator}" +
				    $"{GetLocalizedString("sure")}", GetLocalizedString("del_all_title"), MessageBoxButton.YesNo,
				    MessageBoxImage.Exclamation, MessageBoxResult.No) == MessageBoxResult.Yes)
			{
				// InvalidateRemoteCapturesCache();
				// RefreshCaptures();

				foreach (var capture in capturesList)
				{
					selectedDbManager.DeleteCapture(capture);
				}

				if (selectedDbManager is MySQLDbManager)
				{
					InvalidateRemoteCapturesCache();
				}

				RefreshCapturesWithFallback();
			}
		}
	}

	public class VisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool isVisible)
			{
				// Console.WriteLine("VisibilityConverter: " + isVisible);
				return isVisible ? Visibility.Visible : Visibility.Collapsed;
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
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
