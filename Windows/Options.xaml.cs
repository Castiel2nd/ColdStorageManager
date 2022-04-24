using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Windows;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Markup;
using System.Xml;
using FontAwesome6;
using static ColdStorageManager.Globals;

namespace ColdStorageManager
{
	/// <summary>
	/// Interaction logic for Options.xaml
	/// </summary>
	public partial class Options : Window
	{
		private static Options instance = null;

		private ResourceDictionary currentLangDictionary;

		public static Options GetInstance()
		{
			if(instance == null)
			{
				return new Options();
			}
			else
			{
				return instance;
			}
		}


		private Options()
		{
			SetIcon(this, EFontAwesomeIcon.Solid_Gear);
			Owner = Globals.mainWindow;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			InitializeComponent();
			LoadSettingsAfterUI();
			GetAvalaibleLanguages();
		}

		private void LoadSettingsAfterUI()
		{
			// general
			switch (startupWindowLocation)
			{
				case ("center"):
					cmbWindowStartupLocation.SelectedIndex = 0;
					break;
				case ("remember"):
					cmbWindowStartupLocation.SelectedIndex = 1;
					break;
				default:
					cmbWindowStartupLocation.SelectedIndex = -1;
					break;
			}

			// exceptions
			AppSettingsSection section = Globals.configFile.Sections.Get("exceptions") as AppSettingsSection;
			exceptionPathsEnableCb.IsChecked = bool.Parse(section.Settings["exceptionPathsEnableCb"].Value);
			exceptionsLV.ItemsSource = Globals.exceptionPathsOC;
			exceptionFileTypesCaptureEnableCb.IsChecked = bool.Parse(section.Settings["exceptionFileTypesCaptureEnableCb"].Value);
			exceptionFileTypesCaptureTxtBx.Text = section.Settings["exceptionFileTypesCaptureTxtBx"].Value;
			exceptionFileTypesSearchEnableCb.IsChecked = bool.Parse(section.Settings["exceptionFileTypesSearchEnableCb"].Value);
			exceptionFileTypesSearchTxtBx.Text = section.Settings["exceptionFileTypesSearchTxtBx"].Value;

			//advanced
			debugLoggingCb.IsChecked = bool.Parse(settings["debugLogging"].Value);
		}

		private void okBtn_Click(object sender, RoutedEventArgs e)
		{
			ApplySettings();
			Hide();
		}

		private void applyBtn_Click(object sender, RoutedEventArgs e)
		{
			ApplySettings();
		}

		private void cancelBtn_Click(object sender, RoutedEventArgs e)
		{
			Hide();
		}

		//Dynamically detect available languages
		private void GetAvalaibleLanguages()
		{
			string[] langFiles = Directory.GetFiles("Language", "*" ,SearchOption.TopDirectoryOnly);

			currentLangDictionary = new ResourceDictionary();
			currentLangDictionary.Source =
			new Uri("Language/"+Globals.settings["language"].Value+".xaml", UriKind.RelativeOrAbsolute);

			foreach (string file in langFiles)
			{
				var langResourceDictionary = new ResourceDictionary();
				langResourceDictionary.Source =
					new Uri(file, UriKind.RelativeOrAbsolute);

				ComboBoxItem cmbxItem = new ComboBoxItem();

				//display friendly language name, or the file's own declared name if not available
				if(langResourceDictionary["langCode"] == null)
				{
					continue;
				}else if(currentLangDictionary[langResourceDictionary["langCode"]] == null)
				{
					if(langResourceDictionary["langName"] == null)
					{
						continue;
					}
					cmbxItem.Content = langResourceDictionary["langName"];
				}
				else
				{
					cmbxItem.Content = currentLangDictionary[langResourceDictionary["langCode"]];
				}

				cmbxItem.Tag = langResourceDictionary["langCode"];
				cmbLang.Items.Add(cmbxItem);
				if ((string)currentLangDictionary["langCode"] == (string)langResourceDictionary["langCode"])
				{
					//Console.WriteLine(langResourceDictionary["langCode"]);
					cmbLang.SelectedValue = langResourceDictionary["langCode"];
				}
			}
		}

		private void ApplySettings()
		{
			var settings = Globals.settings;
			var configFile = Globals.configFile;

			// general
			switch (cmbWindowStartupLocation.SelectedIndex)
			{
				case (0):
					startupWindowLocation = "center";
					break;
				case (1):
					startupWindowLocation = "remember";
					break;
				default:
					startupWindowLocation = "remember";
					break;
			}
			settings["startupWindowLocation"].Value = startupWindowLocation;

			//language
			string selectedLang = cmbLang.SelectedValue.ToString();
			if(selectedLang != Globals.settings["language"].Value)
			{
				string prev = Globals.settings["language"].Value;
				Globals.settings["language"].Value = selectedLang;
				LoadLanguage(prev);
				cmbLang.Items.Clear();
				GetAvalaibleLanguages();
			}
			
			// exceptions
			AppSettingsSection section = Globals.configFile.Sections.Get("exceptions") as AppSettingsSection;
			section.Settings["exceptionPathsEnableCb"].Value = (exceptionPathsEnableCb.IsChecked == true).ToString();
			Globals.ExceptionPathsEnable = exceptionPathsEnableCb.IsChecked == true;
			Globals.ExceptionFileTypesCaptureEnable = exceptionFileTypesCaptureEnableCb.IsChecked == true;
			Globals.ExceptionFileTypesSearchEnable = exceptionFileTypesSearchEnableCb.IsChecked == true;
			section.Settings["exceptionsList"].Value = String.Join('\n', Globals.exceptionPathsOC);
			section.Settings["exceptionFileTypesCaptureEnableCb"].Value = (exceptionFileTypesCaptureEnableCb.IsChecked == true).ToString();
			section.Settings["exceptionFileTypesSearchEnableCb"].Value = (exceptionFileTypesSearchEnableCb.IsChecked == true).ToString();
			section.Settings["exceptionFileTypesCaptureTxtBx"].Value = exceptionFileTypesCaptureTxtBx.Text;
			section.Settings["exceptionFileTypesSearchTxtBx"].Value = exceptionFileTypesSearchTxtBx.Text;
			Globals.exceptionFileTypesCaptureList.Clear();
			Globals.exceptionFileTypesCaptureList.AddRange(Globals.GetFileTypesFromString(exceptionFileTypesCaptureTxtBx.Text));
			Globals.exceptionFileTypesSearchList.Clear();
			Globals.exceptionFileTypesSearchList.AddRange(Globals.GetFileTypesFromString(exceptionFileTypesSearchTxtBx.Text));

			//advanced
			settings["debugLogging"].Value = (exceptionPathsEnableCb.IsChecked == true).ToString();

			//saving settings
			configFile.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
		}

		private void AddException_OnClick(object sender, RoutedEventArgs e)
		{
			string entry = addExceptionTxtBx.Text.Trim();
			if (entry.Length != 0)
			{
				Globals.exceptionPathsOC.Add(entry);
			}
		}

		private void DeleteException_OnClick(object sender, RoutedEventArgs e)
		{
			if (exceptionsLV.SelectedIndex != -1)
			{
				Globals.exceptionPathsOC.RemoveAt(exceptionsLV.SelectedIndex);
			}
		}
	}
}
