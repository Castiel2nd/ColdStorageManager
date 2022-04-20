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

namespace ColdStorageManager
{
	/// <summary>
	/// Interaction logic for RegisterControlFrame.xaml
	/// </summary>
	public partial class RegisterControlFrame : UserControl
	{
		public RegisterControlFrame()
		{
			InitializeComponent();
		}

		private void CreateTable_OnSelected(object sender, RoutedEventArgs e)
		{
			TabItem tab = sender as TabItem;
		}

		private void AddColCmd_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			CreateTableControl.AddColumn();
		}

		private void RemoveColCmd_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			CreateTableControl.RemoveColumn();
		}
	}
}
