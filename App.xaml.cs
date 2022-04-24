using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Management;
using System.Text.RegularExpressions;
using static ColdStorageManager.Globals;
using static ColdStorageManager.Logger;

namespace ColdStorageManager
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private Dictionary<string, string> defaultSettings = new Dictionary<string, string>()
		{
			{ "language","en-US" },
			{ "startupWindowState","normal" },
			{ "startupWindowLocation","remember" },
			{ "startupWindowLocationCoords","0,0" },
			{ "debugLogging","False" },
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
					"mysqlConnectionProfiles", new Dictionary<string, string>() {}
				}
			};

		private void App_OnStartup(object sender, StartupEventArgs e)
		{
			LoadSettings();
			GetDriveInfo();
			new MainWindow().Show();
		}

		private void LoadSettings()
		{
			//Initializing vars
			var configFileMap = new ExeConfigurationFileMap();
			configFileMap.ExeConfigFilename = Globals.configFileName;

			configFile = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
			settings = configFile.AppSettings.Settings;

			CheckSettings();

			//Main section
			debugLogging = bool.Parse(settings["debugLogging"].Value);
		}

		//checks the settings in the configuration file, and if any are not present, it loads the default value for it
		//the settings and configFile variables must already be initialized
		private void CheckSettings()
		{
			AppSettingsSection section;
			try
			{
				//configFile.Sections.Add("general", new AppSettingsSection());
				//AppSettingsSection sec = configFile.Sections.Get("general") as AppSettingsSection;
				//sec.Settings.Add("hello", "test");

				//load default settings
				foreach (KeyValuePair<string, string> setting in defaultSettings)
				{
					if (settings[setting.Key] == null)
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
			if (Globals.settings["language"].Value != "en-US")
			{
				LoadLanguage("en-US");
			}
		}

		//gets info from WMI/CIM using the System.Management class
		private void GetDriveInfo()
		{
			stopwatch.Start();

			NVMeQueries NVMeQueries = new NVMeQueries();

			ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM CIM_DiskDrive");

			if (Globals.physicalDrives != null)
			{
				Globals.physicalDrives.Clear();
			}
			else
			{
				Globals.physicalDrives = new List<PhysicalDrive>();
			}

			foreach (ManagementBaseObject baseObject in searcher.Get())
			{
				if (baseObject != null)
				{
					PhysicalDrive physicalDrive = new PhysicalDrive(
						(string)baseObject["Model"],
						(string)baseObject["SerialNumber"],
						(ulong)baseObject["Size"],
						(uint)baseObject["Index"]
					);
					//Console.WriteLine(physicalDrive);

					if (debugLogging)
					{
						LogAction($"Detected drive { physicalDrive.Model}.");
					}

					//NVMe detection
					if (((string)baseObject["PNPDeviceID"]).Contains("NVME", StringComparison.OrdinalIgnoreCase) || physicalDrive.Model.Contains("NVME", StringComparison.OrdinalIgnoreCase))
					{
						physicalDrive.isNVMe = true;
						string sn = NVMeQueries.GetSerialNumber((int)physicalDrive.Index);
						if (sn.Equals("NotFound"))
						{
							physicalDrive.NVMeSerialNumberDetectionFail = true;

							if (debugLogging)
							{
								LogAction($"Drive {physicalDrive.Model} is potentially NVMe, however the serial number couldn't be obtained.", WARNING);
							}
						}
						else
						{
							physicalDrive.SerialNumber = sn;
						}
					}

					Globals.physicalDrives.Add(physicalDrive);
				}
			}
			//sort by index
			Globals.physicalDrives.Sort((PhysicalDrive pd1, PhysicalDrive pd2) => pd1.Index.CompareTo(pd2.Index));
			//foreach (var VARIABLE in Globals.physicalDrives)
			//{
			//	Console.WriteLine(VARIABLE);
			//}

			//querying for info to match logical drives to physical disks
			//this and most things onwards should probably be done via System.IO.DriveInfo class
			List<(uint DiskIndex, uint PartIndex, string Letter)> LogicalToPartition =
				new List<(uint DiskIndex, uint PartIndex, string Letter)>();

			searcher.Query = new ObjectQuery("SELECT * FROM CIM_LogicalDiskBasedOnPartition");

			foreach (ManagementBaseObject baseObject in searcher.Get())
			{
				if (baseObject != null)
				{
					string diskPartition = Regex.Match((string)baseObject["Antecedent"], @"Disk #\d+, Partition #\d+")
						.Value;
					uint diskIndex, partIndex;
					var ints = Regex.Matches(diskPartition, @"\d+");
					diskIndex = uint.Parse(ints[0].Value);
					partIndex = uint.Parse(ints[1].Value);
					string dependent = (string)baseObject["Dependent"];
					string letter = dependent.Substring(dependent.Length - 3, 2);
					LogicalToPartition.Add((diskIndex, partIndex, letter));
				}
			}

			//getting the logical disks and creating Partitions
			searcher.Query = new ObjectQuery("SELECT * FROM CIM_LogicalDisk");

			foreach (ManagementBaseObject baseObject in searcher.Get())
			{
				if (baseObject != null)
				{
					string letter = (string)baseObject["Name"];
					foreach (var tuple in LogicalToPartition)
					{
						if (letter == tuple.Letter)
						{
							foreach (var phDisk in Globals.physicalDrives)
							{
								if (phDisk.Index == tuple.DiskIndex)
								{
									Partition partition = new Partition(
										(ulong)baseObject["Size"],
										(ulong)baseObject["FreeSpace"],
										letter,
										null,
										tuple.DiskIndex,
										tuple.PartIndex,
										phDisk
									);

									if (debugLogging)
									{
										LogAction($"Found partition {partition}.");
									}

									phDisk.Partitions.Add(partition);
								}
							}
						}
					}
				}
			}

			//getting volume GUIDs
			searcher.Query = new ObjectQuery("SELECT DriveLetter, DeviceID FROM Win32_Volume");
			foreach (ManagementBaseObject baseObject in searcher.Get())
			{
				foreach (var physicalDrive in Globals.physicalDrives)
				{
					bool found = false;
					foreach (var partition in physicalDrive.Partitions)
					{
						if (partition.Letter.Equals((string)baseObject["DriveLetter"]))
						{
							//example DeviceID: \\?\Volume{3802a7b6-e776-4957-8e82-b2a1a0ab0b18}\
							partition.VolumeGUID = ((string)baseObject["DeviceID"]).Split(Globals.volumeGuidDelimiterArray)[1];
							found = true;
							break;
						}
					}
					if (found)
						break;
				}
			}

			//getting volume labels
			DriveInfo[] drives = DriveInfo.GetDrives();
			foreach (var drive in drives)
			{
				foreach (var physicalDrive in Globals.physicalDrives)
				{
					foreach (var partition in physicalDrive.Partitions)
					{
						if (partition.Letter[0] == drive.Name[0])
						{
							partition.Label = drive.VolumeLabel;
						}
					}
				}
			}

			//sorting partitions by index for each disk
			foreach (var physicalDrive in Globals.physicalDrives)
			{
				physicalDrive.SortPartitionsByIndex();
			}

			stopwatch.Stop();
			LogActionWithStatus($"{GetLocalizedString("drive_detection")} {stopwatch.Elapsed.TotalSeconds.ToString(secondsFormat)}",
								$"Finished detecting drives in {stopwatch.Elapsed.TotalSeconds.ToString(secondsFormat)}");
		}

		private void App_OnExit(object sender, ExitEventArgs e)
		{
			Globals.configFile.Save(ConfigurationSaveMode.Modified);
		}
	}
}
