
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
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>

	public partial class MainWindow : Window
	{
		private bool captureConnSelectionChangedEnabled = false;

		private void LoadSettingsBeforeUI()
		{
			AppSettingsSection section;

			//defaultSettings other than language
			Globals.startupWindowLocation = Globals.settings["startupWindowLocation"].Value;
			Globals.startupWindowLocationCoords =
				Globals.settings["startupWindowLocationCoords"].Value.Split(',').Select(double.Parse).ToArray();
			if ((Globals.startupWindowLocationCoords.All(d => d == 0)) || Globals.startupWindowLocation.Equals("center"))
			{
				WindowStartupLocation = WindowStartupLocation.CenterScreen;
			}
			else if (Globals.startupWindowLocation.Equals("remember"))
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
				dbConnectionProfiles.Add(new DbConnectionProfile(profileName, section.Settings[profileName].Value));
			}

		}

		private void InitVarsBeforeUI()
		{
			Title = $"Cold Storage Manager {version}";
			Console.WriteLine(Title);
			Globals.mainWindow = this;

			//creating data sources
			dataSources.Add(new DataSource(new DbConnectionProfile(GetLocalizedString(ConfigNameOfLocalDS))));
			foreach (var profile in dbConnectionProfiles)
			{
				dataSources.Add(new DataSource(profile));
			}
		}

		public MainWindow()
		{
			LoadSettingsBeforeUI();
			InitVarsBeforeUI();

			InitializeComponent();

			InitVarsAfterUI();
			LoadSettingsAfterUI();

			tableBrowser.GetTableNames();

			RefreshCapturesWithFallback();
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

			LogActionWithStatus(GetLocalizedString("ready"), "Init complete");
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

			//setting selected data source
			selectedDataSource = GetDataSourceByName(section.Settings["selectedCaptureConnection"].Value);
			captureConnCmbx.SelectedItem = selectedDataSource;
			captureConnSelectionChangedEnabled = true;

			//copy all cmbx options setup
			copyFromCmbx.ItemsSource = dataSources;
			copyToCmbx.ItemsSource = dataSources;
			captureConnCmbx.ItemsSource = dataSources;

			//captureSettings
			LoadCaptureSettings();

			//commands
			Commands.SetupCmds();
		}

		public void LoadCaptureSettings()
		{
			AppSettingsSection section = Globals.configFile.Sections.Get("captureSettings") as AppSettingsSection;
			capPropSizeCb.IsChecked = bool.Parse(section.Settings["capPropSizeCb"].Value);
			capPropCreateTimeCb.IsChecked = bool.Parse(section.Settings["capPropCreateTimeCb"].Value);
			capPropLastAccessCb.IsChecked = bool.Parse(section.Settings["capPropLastAccessCb"].Value);
			capPropLastModCb.IsChecked = bool.Parse(section.Settings["capPropLastModCb"].Value);
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
		private void RefreshCapturesWithFallback(int indexOfFallbackDataSource = 0, bool repeatRefresh = true)
		{

			if (!RefreshCaptures())
			{
				captureConnSelectionChangedEnabled = false;
				captureConnCmbx.SelectedIndex = indexOfFallbackDataSource;
				captureConnSelectionChangedEnabled = true;
				selectedDataSource = dataSources[indexOfFallbackDataSource];
				
				if (!RefreshCaptures())
				{
					//if repeated retry is allowed and fallback index was not 0 (local), then try local
					if (repeatRefresh && indexOfFallbackDataSource != 0)
					{
						captureConnSelectionChangedEnabled = false;
						captureConnCmbx.SelectedIndex = 0;
						captureConnSelectionChangedEnabled = true;
						selectedDataSource = dataSources[0];

						//if this failed too, display error msg
						if (!RefreshCaptures())
						{
							captureConnSelectionChangedEnabled = false;
							captureConnCmbx.SelectedIndex = -1;
							captureConnSelectionChangedEnabled = true;
							capturesDisplay.Clear();
							capturesDisplay.Add(new CapturePlaceholder(GetLocalizedString("could_not_connect_placeholder")));
						}
					}
					else // if repeated retry isn't allowed or fallback was 0, display error msg
					{
						captureConnSelectionChangedEnabled = false;
						captureConnCmbx.SelectedIndex = -1;
						captureConnSelectionChangedEnabled = true;
						capturesDisplay.Clear();
						capturesDisplay.Add(new CapturePlaceholder(GetLocalizedString("could_not_connect_placeholder")));
					}
				}
			}
		}

		//returns whether it could get the captures
		private bool RefreshCaptures()
		{
			List<Capture> captures = selectedDataSource?.GetCaptures();

			if (captures == null)
			{
				return false;
			}

			List<CaptureBase> capturesDisplayList = GetCapturesDisplayListFromCapturesList(captures);
			MatchCapturesDisplayListToPartitions(capturesDisplayList);

			capturesDisplay.Clear();
			capturesDisplay.AddRange(capturesDisplayList);

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

			logPopup.HorizontalOffset = -statusBtnPosition.X;
			logPopup.VerticalOffset = mainStatusBarPosition.Y - statusBtnPosition.Y - 1;
		}

		// =====================================
		// **** EVENT HANDLERS ****
		// =====================================

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
				Globals.fileDialogEntryTree.AddRange(Globals.GetFileSystemEntries(selected.Letter + "\\"));

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
				Globals.selectedDataSource.UpdateCaptureById(newCapture);
			}
			else
			{
				Globals.selectedDataSource.SaveCapture(newCapture);
			}


			//CFSHandler.WriteCFS("test", driveTxtBx.Text, nicknameTxtBx.Text);
			LogActionWithStatus(GetLocalizedString("capture_successful") + Globals.selectedPartition.Letter + " [" +
							 Globals.selectedPartition.Label + "]", $"Successfully captured {Globals.selectedPartition.Letter} [{Globals.selectedPartition.Label}]");

			RefreshCaptures();
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
					SetSectionSetting("searchSettings", "selectedCaptureConnection", ConfigNameOfLocalDS);
					selectedDataSource = GetDataSourceByName(GetLocalizedString(ConfigNameOfLocalDS));
				}
				else if (captureConnCmbx.SelectedIndex == -1)
				{
					captureConnCmbx.SelectedIndex = 0;
					return;
				}
				else
				{
					SetSectionSetting("searchSettings", "selectedCaptureConnection", e.AddedItems[0].ToString());
					selectedDataSource = GetDataSourceByName(e.AddedItems[0].ToString());
				}



				//if refreshing failed for some reason, fall back to previous connection
				if (e.RemovedItems.Count > 0) //there will be 0 removed items if there wasn't anything selected before
				{
					RefreshCapturesWithFallback(dataSources.IndexOf(e.RemovedItems[0] as DataSource));
				}
				else // if there was nothing selected, try local as fallback
				{
					RefreshCapturesWithFallback();
				}

				tableBrowser.Refresh();
			}
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
					bool fromJustConnected = false; //if Source DS was used for the first time, disconnect after 
					DataSource from, to;

					from = GetDataSourceByName(copyFromCmbx.SelectedValue.ToString());
					to = GetDataSourceByName(copyToCmbx.SelectedValue.ToString());

					if (!from.IsConnected)
					{
						if (!from.ConnectAndCreateTable())
						{
							LogDbActionWithStatus(GetLocalizedString("db_copy_all_fail"), "Copy all failed. There was an error connecting to the source db.", ERROR);
							return;
						}

						fromJustConnected = true;
					}

					if (!to.IsConnected)
					{
						if (!to.ConnectAndCreateTable())
						{
							LogDbActionWithStatus(GetLocalizedString("db_copy_all_fail"), "Copy all failed. There was an error connecting to the destination db.", ERROR);
							return;
						}
					}

					//both connected
					var captures = from.GetCaptures();
					foreach (var capture in captures)
					{
						to.SaveCapture(capture);
					}

					if (fromJustConnected)
					{
						from.CloseConnection();
					}

					to.InvalidateCache();

					if (to == selectedDataSource)
					{
						RefreshCapturesWithFallback();
					}

					LogDbActionWithStatus($"{GetLocalizedString("db_copy_all_suc")}  ",
						$"Copy all succeeded. {GetLocalizedString("source")}: {from.Name}, {GetLocalizedString("dest")}: {to.Name}");
				}
				else
				{
					LogActionWithStatusSameMsg("Copy From and To cannot be the same!!", WARNING);
				}
			}
			else
			{
				LogActionWithStatusSameMsg("You must select a connection for From and To as well!!", WARNING);
			}
		}

		private void CopyFromCmbx_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (copyFromListEventsEnabled)
			{
				if (e.AddedItems.Count > 0) // if there are no added items, the selected item has been removed
				{
					if (e.RemovedItems.Count > 0 && e.AddedItems[0].ToString().Equals(copyToCmbx.SelectedValue))
					{
						copyToListEventsEnabled = false;
						copyToCmbx.SelectedValue = e.RemovedItems[0].ToString();
						copyToListEventsEnabled = true;
					}
					else if (e.AddedItems[0].ToString().Equals(copyToCmbx.SelectedValue))
					{
						copyToListEventsEnabled = false;
						copyToCmbx.SelectedIndex = -1;
						copyToListEventsEnabled = true;
					}
				}
			}
		}

		private void CopyToCmbx_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (copyToListEventsEnabled)
			{
				if (e.AddedItems.Count > 0) // if there are no added items, the selected item has been removed
				{
					if (e.RemovedItems.Count > 0 && e.AddedItems[0].ToString().Equals(copyFromCmbx.SelectedValue))
					{
						copyFromListEventsEnabled = false;
						copyFromCmbx.SelectedValue = e.RemovedItems[0].ToString();
						copyFromListEventsEnabled = true;
					}
					else if (e.AddedItems[0].ToString().Equals(copyFromCmbx.SelectedValue))
					{
						copyFromListEventsEnabled = false;
						copyFromCmbx.SelectedIndex = -1;
						copyFromListEventsEnabled = true;
					}
				}
			}
		}

		private void DeleteBtn_Click(object sender, RoutedEventArgs e)
		{
			if (trvCaptures.SelectedItem is Capture capture)
			{
				Globals.selectedDataSource.DeleteCapture(capture);

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

				selectedDataSource.DeleteAllCaptures();

				RefreshCapturesWithFallback();
			}
		}

		private void AddColCmd_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			CreateTableControl.AddColumn();
		}

		private void RemoveColCmd_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			CreateTableControl.RemoveColumn();
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

		// =====================================
		// **** EVENT HANDLERS END ****
		// =====================================

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
}
