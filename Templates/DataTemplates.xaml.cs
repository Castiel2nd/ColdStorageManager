using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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
	}
}
