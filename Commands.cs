using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace ColdStorageManager
{
	public static class Commands
	{
		public static RoutedCommand DataTableCellEditSwitchCmd = new RoutedCommand();
		public static RoutedCommand DataTableAddRowCmd = new RoutedCommand();
		public static RoutedCommand DataTableDeleteRowCmd = new RoutedCommand();
		public static RoutedCommand CreateTableAddColCmd = new RoutedCommand();
		public static RoutedCommand CreateTableRemoveColCmd = new RoutedCommand();

		public static void SetupCmds()
		{
			DataTableCellEditSwitchCmd.InputGestures.Add(new KeyGesture(Key.Tab));
			DataTableAddRowCmd.InputGestures.Add(new KeyGesture(Key.Q, ModifierKeys.Control));
			DataTableDeleteRowCmd.InputGestures.Add(new KeyGesture(Key.Delete));
			CreateTableAddColCmd.InputGestures.Add(new KeyGesture(Key.Add));
			CreateTableRemoveColCmd.InputGestures.Add(new KeyGesture(Key.Subtract));
		}
	}
}
