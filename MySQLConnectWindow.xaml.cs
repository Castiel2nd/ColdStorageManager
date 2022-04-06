using System;
using System.Collections.Generic;
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
using static ColdStorageManager.Globals;

namespace ColdStorageManager
{
	/// <summary>
	/// Interaction logic for MySQLConnectWindow.xaml
	/// </summary>
	public partial class MySQLConnectWindow : Window
	{
		private MySQLDbConnectionProfile selectedProfile;
		private string prevSelectedProfileName;
		private MainWindow parent;
		private bool selectionChangedEnabled = true;

		public MySQLConnectWindow(MainWindow mainWindow)
		{
			parent = mainWindow;
			Owner = parent;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			InitializeComponent();
			SetProfileList();

			if (connProfileCmbx.Items.Count != 0)
			{
				connProfileCmbx.SelectedIndex = 0;
			}

			ShowDialog();
		}

		private void SetProfileList()
		{
			selectionChangedEnabled = false;

			connProfileCmbx.Items.Clear();
			foreach (var profile in mySqlDbConnectionProfiles)
			{
				connProfileCmbx.Items.Add(profile.ProfileName);
			}

			selectionChangedEnabled = true;
		}

		private void DisplayError(string errorMsg, string caption)
		{
			MessageBox.Show(this, errorMsg,
				caption, MessageBoxButton.OK, MessageBoxImage.Error,
				MessageBoxResult.OK);
		}

		private MySQLDbConnectionProfile CreateConnectionProfile()
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

			return new MySQLDbConnectionProfile(profileName, host, port, dbName, username, pw);
		}

		private bool SaveConnectionProfile()
		{
			var profile = CreateConnectionProfile();
			if (profile != null)
			{
				if (connProfileCmbx.SelectedIndex == -1)
				{
					mySqlDbConnectionProfiles.Add(profile);
					mainWindow.AddDisplayConnectionName(profile.ProfileName);
				}
				else if (profile.ProfileName.Equals(mySqlDbConnectionProfiles[connProfileCmbx.SelectedIndex].ProfileName))
				{
					if (mySqlDbConnectionProfiles[connProfileCmbx.SelectedIndex].Equals(profile))
					{
						return true;
					}
					else
					{
						mySqlDbConnectionProfiles.RemoveAt(connProfileCmbx.SelectedIndex);
						mySqlDbConnectionProfiles.Add(profile);
					}
				}
				else
				{
					mySqlDbConnectionProfiles.Add(profile);
					mainWindow.AddDisplayConnectionName(profile.ProfileName);
				}
				SetProfileList();
				connProfileCmbx.SelectedIndex = mySqlDbConnectionProfiles.IndexOf(profile);

				SaveMySQLDbConnectionProfilesToMem();
				WriteConfigFileToDisk();

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
			if (connProfileCmbx.SelectedIndex != -1)
			{
				if (activeMySQLDbManager != null &&
				    activeMySQLDbManager.Profile.Equals(mySqlDbConnectionProfiles[connProfileCmbx.SelectedIndex]))
				{
					if (activeMySQLDbManager.IsConnected)
					{
						activeMySQLDbManager.CloseConnection();
					}

					activeMySQLDbManager = null;
				}

				int selectedIndex = connProfileCmbx.SelectedIndex;
				mainWindow.RemoveDisplayConnectionName(mySqlDbConnectionProfiles[connProfileCmbx.SelectedIndex].ProfileName);
				mySqlDbConnectionProfiles.RemoveAt(connProfileCmbx.SelectedIndex);
				selectedProfile = null;
				SetProfileList();

				SaveMySQLDbConnectionProfilesToMem();
				WriteConfigFileToDisk();

				if (connProfileCmbx.Items.Count > 0 && selectedIndex + 1 <= connProfileCmbx.Items.Count)
				{
					connProfileCmbx.SelectedIndex = selectedIndex;
				}else if (connProfileCmbx.Items.Count > 0)
				{
					connProfileCmbx.SelectedIndex = connProfileCmbx.Items.Count - 1;
				}
				else
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

		private void SetDisplayProfileData(MySQLDbConnectionProfile profile)
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
				selectedProfile = mySqlDbConnectionProfiles[connProfileCmbx.SelectedIndex];
				SetDisplayProfileData(selectedProfile);
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
