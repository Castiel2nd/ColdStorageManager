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
	/// Interaction logic for RegControlCreateTable.xaml
	/// </summary>
	public partial class RegControlCreateTable : UserControl
	{
		private Stack<RegControlColumnCreator> _columnCreators = new Stack<RegControlColumnCreator>();
		public RegControlCreateTable()
		{
			InitializeComponent();
			AddColumn();
		}

		private void AddColumn()
		{
			var column = new RegControlColumnCreator();
			_columnCreators.Push(column);
			ColumnPanel.Children.Add(column);
		}

		private void RemoveColumn()
		{
			ColumnPanel.Children.Remove(_columnCreators.Pop());
		}

		private void AddBtn_OnClick(object sender, RoutedEventArgs e)
		{
			AddColumn();
		}
		private void RemoveBtn_OnClick(object sender, RoutedEventArgs e)
		{
			RemoveColumn();
		}

		private void CreateBtn_OnClick(object sender, RoutedEventArgs e)
		{
			List<ColumnInfo> columnInfos = new List<ColumnInfo>();

			foreach (var regControlColumnCreator in _columnCreators)
			{
				columnInfos.Add(regControlColumnCreator.GetValue());
			}

			selectedDataSource.CreateTable(TableNameTxtBox.Text, columnInfos);
		}

		
	}
}
