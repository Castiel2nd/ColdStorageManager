using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Markup;
using System.Xml;

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
			Owner = Globals.mainWindow;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			InitializeComponent();
			GetAvalaibleLanguages();

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

			//language
			string selectedLang = cmbLang.SelectedValue.ToString();
			if(selectedLang != Globals.settings["language"].Value)
			{
				string prev = Globals.settings["language"].Value;
				Globals.settings["language"].Value = selectedLang;
				Globals.mainWindow.LoadLanguage(prev);
				cmbLang.Items.Clear();
				GetAvalaibleLanguages();
			}
			

			//saving settings
			configFile.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
		}

		private void SaveResourceDictionary(string path, ResourceDictionary resourceDictionary)
		{
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			XmlWriter writer = XmlWriter.Create(path, settings);
			XamlWriter.Save(resourceDictionary, writer);
		}

	}
}
