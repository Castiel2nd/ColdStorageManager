using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using ColdStorageManager.Annotations;

namespace ColdStorageManager.ConfigBindings
{
	public class SearchSettingsSection: ConfigurationSection, INotifyPropertyChanged
	{
		private bool sizeEnabled;

		public bool SizeEnabled
		{
			get
			{
				return sizeEnabled;
			}
			set
			{
				sizeEnabled = value;
				Cu
				OnPropertyChanged(nameof(SizeEnabled));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
