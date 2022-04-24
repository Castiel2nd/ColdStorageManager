using System;
using System.Collections.Generic;
using System.Drawing;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ColdStorageManager.DBManagers;
using ColdStorageManager.Models;
using FontAwesome6;
using FontAwesome6.Svg.Converters;
using static ColdStorageManager.Globals;

namespace ColdStorageManager
{
	/// <summary>
	/// Interaction logic for MySQLConnectWindow.xaml
	/// </summary>
	public partial class MySQLConnectWindow : Window
	{
		private DbConnectionProfile selectedProfile;
		private string prevSelectedProfileName;
		private MainWindow parent;
		private bool selectionChangedEnabled = true;

		public MySQLConnectWindow(MainWindow mainWindow)
		{
			SetIcon(this, EFontAwesomeIcon.Solid_Database);

			parent = mainWindow;
			Owner = parent;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			InitializeComponent();
			connProfileCmbx.ItemsSource = dbConnectionProfiles;

			if (connProfileCmbx.Items.Count != 0)
			{
				connProfileCmbx.SelectedIndex = 0;
			}

			ShowDialog();
		}

		// private void SetProfileList()
		// {
		// 	selectionChangedEnabled = false;
		//
		// 	connProfileCmbx.Items.Clear();
		// 	foreach (var profile in dbConnectionProfiles)
		// 	{
		// 		connProfileCmbx.Items.Add(profile.ProfileName);
		// 	}
		//
		// 	selectionChangedEnabled = true;
		// }

		private void DisplayError(string errorMsg, string caption)
		{
			MessageBox.Show(this, errorMsg,
				caption, MessageBoxButton.OK, MessageBoxImage.Error,
				MessageBoxResult.OK);
		}

		private DbConnectionProfile CreateConnectionProfile()
		{
			string profileName = connNameTxtbox.Text.Trim(),
				host = hostnameTxtbox.Text.Trim(),
				portTxt = portTxtbox.Text.Trim(),
				dbName = dbNameTxtbox.Text.Trim(),
				username = usernameTxtbox.Text.Trim(),
				pw = passwordBox.Password.Trim();

			bool isProfileNameEmpty = profileName.Equals(""),
				isHostEmpty = host.Equals(""),
				isPortEmpty = portTxt.Equals(""),
				isDbNameEmpty = dbName.Equals(""),
				isUsernameEmpty = username.Equals("");

			if (isProfileNameEmpty || isHostEmpty || isPortEmpty || isDbNameEmpty || isUsernameEmpty)
			{
				DisplayError(GetLocalizedString("error_empty_fields") + (isProfileNameEmpty ? LineSeparator+GetLocalizedString("connection_name").TrimEnd(':') : "") +
																			(isHostEmpty ? LineSeparator + GetLocalizedString("host_loc").TrimEnd(':') : "") +
																			(isPortEmpty ? LineSeparator + GetLocalizedString("port").TrimEnd(':') : "") +
																			(isDbNameEmpty ? LineSeparator + GetLocalizedString("database").TrimEnd(':') : "") +
																			(isUsernameEmpty ? LineSeparator + GetLocalizedString("username").TrimEnd(':') : ""),
					GetLocalizedString("error_empty_fields_title"));

				return null;
			}

			bool isPortWrongFormat = false, isPortOverflown = false;

			ushort port = 0;

			try
			{
				port = UInt16.Parse(portTxtbox.Text);
			}
			catch (ArgumentNullException)
			{

			}
			catch (FormatException)
			{
				isPortWrongFormat = true;
			}
			catch (OverflowException)
			{
				isPortOverflown = true;
			}

			if (isPortWrongFormat || isPortOverflown)
			{
				DisplayError(
					GetLocalizedString("error_wrong_port_format_or_value") + "\n" +
					GetLocalizedString("error_wrong_port_format_or_value_pt2"),
					GetLocalizedString("error_wrong_port_format_or_value_title"));
				return null;
			}

			return new DbConnectionProfile(profileName, host, port, dbName, username, pw);
		}

		// returns whether the profile could be saved
		private bool SaveConnectionProfile()
		{
			var profile = CreateConnectionProfile();
			if (profile != null)
			{
				if (connProfileCmbx.SelectedIndex == -1) // no profile is selected, just add it
				{
					dbConnectionProfiles.Add(profile);
				}
				else if (profile.ProfileName.Equals(dbConnectionProfiles[connProfileCmbx.SelectedIndex].ProfileName)) // if profile saved already exists
				{
					if (dbConnectionProfiles[connProfileCmbx.SelectedIndex].Equals(profile)) // check if it's been modified
					{
						return true;
					}
					else //if yes, remove the old one, and add the new
					{
						selectionChangedEnabled = false;
						dbConnectionProfiles.RemoveAt(connProfileCmbx.SelectedIndex);
						dbConnectionProfiles.Add(profile);
						selectionChangedEnabled = true;
					}
				}
				else // if there was a different profile selected, still just add it
				{
					dbConnectionProfiles.Add(profile);
				}
				connProfileCmbx.SelectedIndex = dbConnectionProfiles.IndexOf(profile);

				SaveMySQLDbConnectionProfilesToMem();
				WriteConfigFileToDisk();

				//create DataSource
				dataSources.Add(new DataSource(profile));

				return true;
			}

			return false;
		}

		private void SaveAndConnect_OnClick(object sender, RoutedEventArgs e)
		{
			if (SaveConnectionProfile())
			{

			}
		}

		private void Save_OnClick(object sender, RoutedEventArgs e)
		{
			SaveConnectionProfile();
		}

		private void Delete_OnClick(object sender, RoutedEventArgs e)
		{
			if (connProfileCmbx.SelectedIndex != -1) // if there's something selected
			{
				int selectedIndex = connProfileCmbx.SelectedIndex;

				//delete the corresponding DataSource
				var ds = GetDataSourceByName(dbConnectionProfiles[selectedIndex].ProfileName);
				if (ds != null)
				{
					ds.CloseConnection();
					dataSources.Remove(ds);
				}

				dbConnectionProfiles.RemoveAt(connProfileCmbx.SelectedIndex);
				selectedProfile = null;

				SaveMySQLDbConnectionProfilesToMem();
				WriteConfigFileToDisk();

				if (connProfileCmbx.Items.Count > 0 && selectedIndex < connProfileCmbx.Items.Count) // if there are still profiles and the same index is available, use that
				{
					connProfileCmbx.SelectedIndex = selectedIndex;
				}
				else if (connProfileCmbx.Items.Count > 0) // if there are still profiles, use the last one
				{
					connProfileCmbx.SelectedIndex = connProfileCmbx.Items.Count - 1;
				}
				else // if the last profile has just been deleted, reset the fields
				{
					Reset_OnClick(null, null);
				}
			}
		}

		private void Reset_OnClick(object sender, RoutedEventArgs e)
		{
			// connProfileCmbx.SelectedIndex = -1;
			connNameTxtbox.Clear();
			hostnameTxtbox.Text = "localhost";
			portTxtbox.Text = "3306";
			dbNameTxtbox.Clear();
			usernameTxtbox.Clear();
			passwordBox.Clear();
		}

		private void Cancel_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void SetDisplayProfileData(DbConnectionProfile profile)
		{
			connNameTxtbox.Text = profile.ProfileName;
			hostnameTxtbox.Text = profile.Host;
			dbNameTxtbox.Text = profile.DatabaseName;
			usernameTxtbox.Text = profile.UID;
			passwordBox.Password = profile.Password;
		}

		private void ConnProfileCmbx_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (selectionChangedEnabled)
			{
				if (e.AddedItems.Count > 0)
				{
					selectedProfile = dbConnectionProfiles[connProfileCmbx.SelectedIndex];
					SetDisplayProfileData(selectedProfile);
				}
			}
		}

		private void Test_OnClick(object sender, RoutedEventArgs e)
		{
			var profile = CreateConnectionProfile();
			var testDbManager = new MySQLDbManager(profile);
			testDbManager.TestConnection();
		}
	}
}
