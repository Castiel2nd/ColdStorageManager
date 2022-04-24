using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ColdStorageManager.Models;
using static ColdStorageManager.Globals;

namespace ColdStorageManager.Controls
{
	/// <summary>
	/// Interaction logic for RegControlTableBrowser.xaml
	/// </summary>
	public partial class RegControlTableBrowser : UserControl
	{
		public RegControlTableBrowser()
		{
			InitializeComponent();

			tableBrowser = this;
		}

		public void Refresh()
		{
			GetTableNames();
			TableDisplayArea.Children.Clear();
		}

		public void GetTableNames()
		{
			TableNamesSP.Children.Clear();

			var tableNames = selectedDataSource.GetTableNames();

			foreach (var tableName in tableNames)
			{
				Button button = new Button();
				button.Content = tableName;
				button.Click += TableName_OnClick;
				TableNamesSP.Children.Add(button);
			}
		}

		private void TableName_OnClick(object sender, RoutedEventArgs e)
		{
			Button btn = sender as Button;

			TableDisplayArea.Children.Clear();

			string tableName = btn.Content.ToString();

			TableModel table = new TableModel(tableName,
				selectedDataSource.GetColumns(tableName));

			selectedDataSource.SetTableData(ref table);

			TableDisplayArea.Children.Add(new RegControlTableDataBrowser(table));
		}
	}
}
