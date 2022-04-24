using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ColdStorageManager.DBManagers;
using ColdStorageManager.Models;
using Org.BouncyCastle.Bcpg;
using static ColdStorageManager.Globals;
using static ColdStorageManager.Logger;

namespace ColdStorageManager.Controls
{
	/// <summary>
	/// Interaction logic for RegControlTableDataBrowser.xaml
	/// </summary>
	public partial class RegControlTableDataBrowser : UserControl
	{
		private const int DbResNotificationDurationMs = 500;
		private static readonly SolidColorBrush _newRowBackground = new SolidColorBrush(Color.FromRgb(200, 200, 200));
		private static readonly SolidColorBrush _successfulEditRowBackground = new SolidColorBrush(Color.FromRgb(110, 221, 108));
		private static readonly SolidColorBrush _failedEditRowBackground = new SolidColorBrush(Color.FromRgb(221, 108, 108));
		private static Brush _defaultRowBackground;

		//used for indicating db action result after successfully adding a row
		//indication needs to happen upon loading the new row, otherwise it's hard to get the DataGridRow object and there are timing issues as well
		private Brush _notifyBackgroundOnLoad, _afterNotifyBackgroundOnLoad;
		private bool _onRowLoadDbResNotify;
		private int _delay;

		private DataGridCellInfo _currentlyEditedCell;
		private object _dataOnEditBegin;
		private ObservableCollection<DataGridColumn> _columns;
		private TableModel _table;
		private bool _newRowAdded = false, _isCellBeingEdited = false, _lastEditOperationSuccessful, _switchedCellByCmd, _editCancelled;

		private static ContextMenu _rowHeaderContextMenu = GetResource("DataGridRowHeaderContextMenu") as ContextMenu,
			_columnHeaderContextMenu = GetResource("DataGridColumnHeaderContextMenu") as ContextMenu,
			_dataAreaContextMenu = GetResource("DataGridDataAreaContextMenu") as ContextMenu,
			_topLeftAreaContextMenu = GetResource("DataGridTopLeftAreaContextMenu") as ContextMenu;

		// private MenuItem[] _rowHeaderContextMenuItems, _columnHeaderContextMenuItems, _dataAreaContextMenuItems, _topLeftAreaContextMenuItems;

		public RegControlTableDataBrowser(TableModel table)
		{
			_table = table;

			InitializeComponent();

			_defaultRowBackground = DataGrid.Background;
			_columns = DataGrid.Columns;

			SetColumns(ref table, ref _columns);

			DataGrid.ItemsSource = table.Data;

			InitContextMenus();

			Focus();
		}

		private void InitContextMenus()
		{
			((MenuItem)_rowHeaderContextMenu.Items[0]).Click += DelRowsCntxMenu_OnClick;

			// if (_rowHeaderContextMenuItems == null)
			// {
			// 	_rowHeaderContextMenuItems = new MenuItem[1];
			//
			// 	var mi = new MenuItem();
			// 	mi.Header = GetLocalizedString("del_selected");
			// 	mi.Click += DelRowsCntxMenu_OnClick;
			// 	_rowHeaderContextMenuItems[0] = mi;
			// }
			//
			// if (_columnHeaderContextMenuItems == null)
			// {
			// 	_columnHeaderContextMenuItems = Array.Empty<MenuItem>();
			//
			// }
			//
			// if (_dataAreaContextMenuItems == null)
			// {
			// 	_dataAreaContextMenuItems = Array.Empty<MenuItem>();
			// }
			//
			// if (_topLeftAreaContextMenuItems == null)
			// {
			// 	_topLeftAreaContextMenuItems = Array.Empty<MenuItem>();
			// }
		}

		public void RefreshTableData()
		{
			selectedDataSource.SetTableData(ref _table);

			DataGrid.ItemsSource = _table.Data;
		}

		private void SetColumns(ref TableModel table, ref ObservableCollection<DataGridColumn> columns)
		{
			for (int i = 0; i<_table.Columns.Length ; i++)
			{
				DataGridColumn column;
				
				switch (table.Columns[i].Type)
				{
					case CSMColumnType.Text:
						var colTxt = new DataGridTextColumn
						{
							Binding = new Binding($"[{i}]")
						};
						column = colTxt;
						break;
					case CSMColumnType.Integer:
						var colInt = new DataGridTextColumn
						{
							Binding = new Binding($"[{i}]")
						};
						column = colInt;
						break;
					default:
						var colDef = new DataGridTextColumn
						{
							Binding = new Binding($"[{i}]")
						};
						column = colDef;
						break;
				}

				//don't modify the Id
				if (table.Columns[i].Name.Equals("id"))
				{
					column.IsReadOnly = true;
				}

				column.Header = table.Columns[i].Name;
				
				_columns.Add(column);
			}
		}

		//indicates the result of a modification operation on a row by changing the background for the specified amount of time
		private void IndicateDbOperationResult(DataGridRow row, Brush notifyBackground, Brush afterNotifyBackground, int milliseconds)
		{
			var progress = ProgressManager.GetUpdateFrontendPropertyProgress<Brush>(row, "Background");

				Task.Run(() =>
				{ 
					progress.Report(notifyBackground);
					Thread.Sleep(milliseconds);
					progress.Report(afterNotifyBackground);
				});
			}

		private int GetDataGridColumnIndexByName(string columnName)
		{
			for (int i = 0; i < _columns.Count; i++)
			{
				if (_columns[i].Header.ToString().Equals(columnName))
				{
					return i;
				}
			}

			return -1;
		}

		private object[] CreateNewRowObject()
		{
			object[] row = new object[_table.Columns.Length];

			for (int i = 0; i < _table.Columns.Length; i++)
			{
				if (_table.Columns[i].IsTextEditable)
				{
					switch (_table.Columns[i].Type)
					{
						case CSMColumnType.Text:
							row[i] = "";
							break;
						case CSMColumnType.Integer:
							row[i] = 0L;
							break;
					}
				}
			}

			return row;
		}

		private void DataGrid_OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
		{
			int rowIndex = e.Row.GetIndex(), colIndex = _columns.IndexOf(e.Column); 
			object id = _newRowAdded ? null : (e.Row.Item as object[])[_table.GetColumnIndexByName("id")];
			string columnName = e.Column.Header.ToString();

			object value = null;

			// Console.WriteLine((e.Row.Item as object[])[_table.GetColumnIndexByName("id")]);
			// Console.WriteLine(columnName);
			// Console.WriteLine($"edited row: {e.Row.GetIndex()} col: {_columns.IndexOf(e.Column)} From: {e.EditingElement.}");

			switch (_table.GetColumnByName(columnName).Type)
			{
				case CSMColumnType.Text:
					TextBox tb = e.EditingElement as TextBox;
					if (_editCancelled)
					{
						_editCancelled = false;
						tb.Text = ((object[])e.Row.Item)[_table.GetColumnIndexByName(columnName)].ToString();
						_lastEditOperationSuccessful = true;
						return;
					}
					value = tb.Text;
					break;
				case CSMColumnType.Integer:
					TextBox tbInt = e.EditingElement as TextBox;
					if (_editCancelled)
					{
						_editCancelled = false;
						tbInt.Text = ((object[])e.Row.Item)[_table.GetColumnIndexByName(columnName)].ToString();
						_lastEditOperationSuccessful = true;
						return;
					}
					value = tbInt.Text;
					break;
			}

			if(!selectedDataSource.TryParse(_table.GetColumnByName(columnName).Type, value))
			{
				e.Cancel = true;
				_lastEditOperationSuccessful = false;

				IndicateDbOperationResult(e.Row, _failedEditRowBackground, _defaultRowBackground, DbResNotificationDurationMs);

				CreatePopup(GetLocalizedString("db_bad_cell_value"), GetDataGridCell(_currentlyEditedCell), ERROR);

				return;
			}

			if (!selectedDataSource.SetCellData(_table.Name, columnName, id, value))
			{
				e.Cancel = true;
				_lastEditOperationSuccessful = false;

				IndicateDbOperationResult(e.Row, _failedEditRowBackground, _defaultRowBackground, DbResNotificationDurationMs);

				return;
			}

			if (_newRowAdded)
			{
				_newRowAdded = false;
				var refreshedRow = selectedDataSource.GetLastInsertedRow(_table.Name, _table.Columns);

				_onRowLoadDbResNotify = true;
				_notifyBackgroundOnLoad = _successfulEditRowBackground;
				_afterNotifyBackgroundOnLoad = _defaultRowBackground;

				_table.ReplaceRow((object[])e.Row.Item, refreshedRow);
				if (_switchedCellByCmd)
				{
					_switchedCellByCmd = false;
					BeginEditingCell(refreshedRow, GetNextEditableColumnIndex(colIndex));
				}
			}
			else
			{
				IndicateDbOperationResult(e.Row, _successfulEditRowBackground, _defaultRowBackground, DbResNotificationDurationMs);
				_isCellBeingEdited = false;
			}

			_lastEditOperationSuccessful = true;
		}

		//begins editing the cell specified by the row object and a column index
		// if the column index is invalid, it does nothing
		private bool BeginEditingCell(object[] row, int columnIndex)
		{
			if (columnIndex < 0 || columnIndex >= _columns.Count)
			{
				return false;
			}

			DataGrid.CurrentCell = new DataGridCellInfo(row, _columns[columnIndex]);
			DataGrid.SelectedCells.Clear();
			DataGrid.SelectedCells.Add(DataGrid.CurrentCell);
			return DataGrid.BeginEdit();
		}

		//gets the index of the next text-editable cell in the current row, from left to right
		// the parameter is the previous column index; -1 represents starting from the beginning
		// returns -1 if there are no more columns
		private int GetNextEditableColumnIndex(int i = -1)
		{
			int retIndex = i;

			do
			{
				retIndex++;
				if (retIndex >= _columns.Count || retIndex < 0)
				{
					return -1;
				}

			} while (!_table.GetColumnByName(_columns[retIndex].Header.ToString()).IsTextEditable);

			return retIndex;
		}

		private void AddRowBtn_OnClick(object sender, RoutedEventArgs e)
		{
			AddRow();
		}

		private void AddRow()
		{
			if (!_newRowAdded)
			{
				_newRowAdded = true;
				DataGrid.Focus();
				object[] row = CreateNewRowObject();
				_table.Data.Add(row);
				// DataGrid.SelectedItem = row;

				BeginEditingCell(row, GetNextEditableColumnIndex());
			}
		}

		private void DataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Console.WriteLine(e.AddedItems);
			// Console.WriteLine(e.AddedItems[0]);
			// Console.WriteLine(e.Source);
			// Console.WriteLine("selectedindex: " + DataGrid.SelectedIndex);
			// Console.WriteLine("selectedCellsCount: " + DataGrid.SelectedCells.Count);
			// var row = (DataGridRow)DataGrid.ItemContainerGenerator.ContainerFromIndex(DataGrid.SelectedIndex);
			// Console.WriteLine(row);
		}

		private void DataGrid_OnLoadingRow(object? sender, DataGridRowEventArgs e)
		{
			if (_onRowLoadDbResNotify)
			{
				_onRowLoadDbResNotify = false;
				IndicateDbOperationResult(e.Row, _notifyBackgroundOnLoad, _afterNotifyBackgroundOnLoad, DbResNotificationDurationMs);
			}

			if (_newRowAdded)
			{
				e.Row.Background = _newRowBackground;
			}
		}

		private void CellEditSwitchCmd_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (_isCellBeingEdited)
			{
				if (_newRowAdded)
				{
					_switchedCellByCmd = true;
					DataGrid.CommitEdit();
				}
				else
				{
					BeginEditingCell((object[])DataGrid.CurrentCell.Item, GetNextEditableColumnIndex(_columns.IndexOf(DataGrid.CurrentCell.Column)));

				}
			}
		}

		private void DataGrid_OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
		{
			_isCellBeingEdited = true;
			_currentlyEditedCell = DataGrid.CurrentCell;
		}

		private void AddRowCmd_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			AddRow();
		}

		private void DeleteSelectedRows()
		{
			int index = _table.GetColumnIndexByName("id");
			List<object[]> rowsToRemove = new List<object[]>();
			foreach (var dataGridSelectedItem in DataGrid.SelectedItems)
			{
				if (dataGridSelectedItem is object[] row)
				{
					if (!selectedDataSource.DeleteRowById(_table.Name, row[index]))
					{
						var dgr = (DataGridRow)(DataGrid.ItemContainerGenerator.ContainerFromItem(row));
						IndicateDbOperationResult(dgr, _failedEditRowBackground, _defaultRowBackground, DbResNotificationDurationMs);
					}
					else
					{
						rowsToRemove.Add(row);
					}
				}
			}

			//must remove rows from ItemsSource after iterating through the SelectedItems, otherwise would cause exception
			foreach (var row in rowsToRemove)
			{
				_table.Data.Remove(row);
			}
		}

		private void DelRowsCntxMenu_OnClick(object sender, RoutedEventArgs e)
		{
			DeleteSelectedRows();
		}

		private void DataGrid_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			var pos = Mouse.GetPosition(DataGrid);

			//RowHeader
			if (pos.X <= DataGrid.RowHeaderActualWidth && pos.Y > DataGrid.ColumnHeaderHeight)
			{
				DataGrid.ContextMenu = _rowHeaderContextMenu;
				// if (DataGrid.ContextMenu != null)
				// {
				// 	DataGrid.ContextMenu.Items.Clear();
				// 	foreach (var menuItem in _rowHeaderContextMenuItems)
				// 	{
				// 		DataGrid.ContextMenu.Items.Add(menuItem);
				// 	}
				// }
			}
			else if (pos.X > DataGrid.RowHeaderActualWidth && pos.Y <= DataGrid.ColumnHeaderHeight)//ColumnHeader
			{
				DataGrid.ContextMenu = _columnHeaderContextMenu;
				if (_columnHeaderContextMenu.Items.Count == 0)
				{
					e.Handled = true;
				}
				// if (DataGrid.ContextMenu != null)
				// {
				// 	DataGrid.ContextMenu.Items.Clear();
				// 	if (_columnHeaderContextMenuItems.Length == 0)
				// 	{
				// 		e.Handled = true;
				// 	}
				// 	foreach (var menuItem in _columnHeaderContextMenuItems)
				// 	{
				// 		DataGrid.ContextMenu.Items.Add(menuItem);
				// 	}
				// }
			}
			else if (pos.X > DataGrid.RowHeaderActualWidth && pos.Y > DataGrid.ColumnHeaderHeight)//DataArea
			{
				DataGrid.ContextMenu = _dataAreaContextMenu;
				if (_dataAreaContextMenu.Items.Count == 0)
				{
					e.Handled = true;
				}
				// if (DataGrid.ContextMenu != null)
				// {
				// 	DataGrid.ContextMenu.Items.Clear();
				// 	if (_dataAreaContextMenuItems.Length == 0)
				// 	{
				// 		e.Handled = true;
				// 	}
				// 	foreach (var menuItem in _dataAreaContextMenuItems)
				// 	{
				// 		DataGrid.ContextMenu.Items.Add(menuItem);
				// 	}
				// }
			}
			else//Select-all area on the top left
			{
				DataGrid.ContextMenu = _topLeftAreaContextMenu;
				if (_topLeftAreaContextMenu.Items.Count == 0)
				{
					e.Handled = true;
				}
				// if (DataGrid.ContextMenu != null)
				// {
				// 	DataGrid.ContextMenu.Items.Clear();
				// 	if (_topLeftAreaContextMenuItems.Length == 0)
				// 	{
				// 		e.Handled = true;
				// 	}
				// 	foreach (var menuItem in _topLeftAreaContextMenuItems)
				// 	{
				// 		DataGrid.ContextMenu.Items.Add(menuItem);
				// 	}
				// }
			}
		}

		private void DeleteRowCmd_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			DeleteSelectedRows();
		}

		private void StopEditCmd_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			_editCancelled = true;
			DataGrid.CommitEdit();
		}

		private void DeleteTable_OnClick(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show(mainWindow,
				    $"{GetLocalizedString("db_del_table_question")} {_table.Name} {GetLocalizedString("from")} {selectedDataSource.Name}?",
				    GetLocalizedString("db_del_table_title"), MessageBoxButton.YesNo, MessageBoxImage.Question,
				    MessageBoxResult.No) == MessageBoxResult.Yes)
			{
				selectedDataSource.DeleteTable(_table.Name);
				tableBrowser.Refresh();
			}
		}
	}
}
