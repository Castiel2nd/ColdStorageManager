using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ColdStorageManager.Templates
{
	public partial class DataTemplates
	{
		private void FSEntryCheckBox_Clicked(object sender, RoutedEventArgs e)
		{
			if (sender is CheckBox checkBox)
			{
				//prevent manually setting the 3rd state
				if (checkBox.IsChecked == null)
				{
					checkBox.IsChecked = false;
				}
			}
		}

		private void ColumnHeaderMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			Console.WriteLine(sender);
			Console.WriteLine(e.Source);
			Console.WriteLine(e.OriginalSource);
		}
	}
}
