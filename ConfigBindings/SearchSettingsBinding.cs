using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Windows.Data;

namespace ColdStorageManager.ConfigBindings
{
	public class SearchSettingsBinding : Binding
	{
		public SearchSettingsBinding()
		{
			Initialize();
		}

		public SearchSettingsBinding(string path)
			: base(path)
		{
			Initialize();
		}

		private void Initialize()
		{
			this.Source = (Globals.configFile.Sections.Get("searchSettings") as SearchSettingsSection);
			this.Mode = BindingMode.TwoWay;
		}
	}
}
