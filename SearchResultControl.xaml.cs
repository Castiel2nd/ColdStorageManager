using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
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
using ColdStorageManager.Models;
using static ColdStorageManager.Globals;

namespace ColdStorageManager
{
	/// <summary>
	/// Interaction logic for SearchResultControl.xaml
	/// </summary>
	public partial class SearchResultControl : UserControl
	{
		private GridViewColumn[] columnReferences;
		private SearchResultCaptureView searchResultCaptureView;
		private bool files, initialExpanson = true;
		public GridViewColumnCollection columns;
		
		public bool preventLoopback, init = true;

		public GridViewColumn[] ColumnReferences
		{
			get
			{
				return columnReferences;
			}
		}
		public SearchResultControl(SearchResultCaptureView searchResultCaptureView, bool files)
		{
			this.files = files;
			this.searchResultCaptureView = searchResultCaptureView;
			InitializeComponent();
			DriveNameTB.Text = searchResultCaptureView.PhDisk.drive_model;
			DriveSnTB.Text = "["+searchResultCaptureView.PhDisk.drive_sn+"]";
			DriveSizeTB.Text = searchResultCaptureView.PhDisk.FormattedSize;
			resultsListView.ItemsSource = searchResultCaptureView.SearchResults;
			columns = (resultsListView.View as GridView).Columns;
			columnReferences = new GridViewColumn[columns.Count];
			for (int i = 0; i < columns.Count; i++)
			{
				columnReferences[i] = columns[i];
			}

			//loading gridViewColumn settings
			//order
			columns.Clear();
			if (files)
			{
				foreach (var columnIndex in fileResultsColumnsOrder)
				{
					columns.Add(columnReferences[columnIndex]);	
				}
				for (int i = 0; i < columnReferences.Length; i++)
				{
					columnReferences[i].Width = fileResultsColumnWidths[i];
				}
			}
			else
			{
				foreach (var columnIndex in dirResultsColumnsOrder)
				{
					columns.Add(columnReferences[columnIndex]);
				}
				for (int i = 0; i < columnReferences.Length; i++)
				{
					columnReferences[i].Width = dirResultsColumnWidths[i];
				}
			}

			columns.CollectionChanged += SyncColumnMoves;
			((INotifyPropertyChanged)columnReferences[0]).PropertyChanged += ColumnWidthChanged0;
			((INotifyPropertyChanged)columnReferences[1]).PropertyChanged += ColumnWidthChanged1;
			((INotifyPropertyChanged)columnReferences[2]).PropertyChanged += ColumnWidthChanged2;
			((INotifyPropertyChanged)columnReferences[3]).PropertyChanged += ColumnWidthChanged3;
			((INotifyPropertyChanged)columnReferences[4]).PropertyChanged += ColumnWidthChanged4;
			((INotifyPropertyChanged)columnReferences[5]).PropertyChanged += ColumnWidthChanged5;
		}

		private void InsertColumn(int index)
		{
			columns?.Add(columnReferences[index]);
		}

		private void fileNameMenuItem_OnChecked(object sender, RoutedEventArgs e)
		{
			InsertColumnOnCheck(0);
		}

		private void fileNameMenuItem_OnUnchecked(object sender, RoutedEventArgs e)
		{
			RemoveColumnOnUncheck(0);
		}

		private void pathMenuItem_OnChecked(object sender, RoutedEventArgs e)
		{
			InsertColumnOnCheck(1);
		}

		private void pathMenuItem_OnUnchecked(object sender, RoutedEventArgs e)
		{
			RemoveColumnOnUncheck(1);
		}

		private void sizeMenuItem_OnChecked(object sender, RoutedEventArgs e)
		{
			InsertColumnOnCheck(2);
		}

		private void sizeMenuItem_OnUnchecked(object sender, RoutedEventArgs e)
		{
			RemoveColumnOnUncheck(2);
		}

		private void creation_timeMenuItem_OnChecked(object sender, RoutedEventArgs e)
		{
			InsertColumnOnCheck(3);
		}

		private void creation_timeMenuItem_OnUnchecked(object sender, RoutedEventArgs e)
		{
			RemoveColumnOnUncheck(3);
		}

		private void access_timeMenuItem_OnChecked(object sender, RoutedEventArgs e)
		{
			InsertColumnOnCheck(4);
		}

		private void access_timeMenuItem_OnUnchecked(object sender, RoutedEventArgs e)
		{
			RemoveColumnOnUncheck(4);
		}

		private void mod_timeMenuItem_OnChecked(object sender, RoutedEventArgs e)
		{
			InsertColumnOnCheck(5);
		}

		private void mod_timeMenuItem_OnUnchecked(object sender, RoutedEventArgs e)
		{
			RemoveColumnOnUncheck(5);
		}

		private void InsertColumnOnCheck(int index)
		{
			if (!init)
			{
				InsertColumn(index);
				SyncEnableColumn(index);
				if (files)
				{
					fileResultsColumnsOrder.Add(index);
				}
				else
				{
					dirResultsColumnsOrder.Add(index);
				}
			}
		}

		private void RemoveColumnOnUncheck(int index)
		{
			columns.Remove(columnReferences[index]);
			SyncDisableColumn(index);
			if (files)
			{
				fileResultsColumnsOrder.Remove(index);
			}
			else
			{
				dirResultsColumnsOrder.Remove(index);
			}
		}

		private void SyncColumnMovesTask(int oldIndex, int newIndex, IProgress<int> progress)
		{
			progress.Report((oldIndex*10) + newIndex);
		}

		private void SyncColumnMoves(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Move)
			{
				if (preventLoopback)
				{
					preventLoopback = false;
				}
				else
				{
					var progressIndicator = new Progress<int>(i =>
					{
						int oldIndex = i / 10, newIndex = i % 10;
						if (files)
						{
							foreach (var entry in Globals.filesFound)
							{
								if (entry != this)
								{
									entry.preventLoopback = true;
									entry.columns.Move(oldIndex, newIndex);
								}
							}
						}
						else
						{
							foreach (var entry in Globals.dirsFound)
							{
								if (entry != this)
								{
								}
							}
						}
					});
					Task.Run(() => SyncColumnMovesTask(e.OldStartingIndex, e.NewStartingIndex, progressIndicator));
					MoveItemInList<int>(e.OldStartingIndex, e.NewStartingIndex, files ? fileResultsColumnsOrder : dirResultsColumnsOrder);
				}
			}
		}

		public void ColumnEnableManual(int index)
		{
			switch (index)
			{
				case 0:
					filenameMI.IsChecked = true;
					break;
				case 1:
					pathMI.IsChecked = true;
					break;
				case 2:
					sizeMI.IsChecked = true;
					break;
				case 3:
					creation_timeMI.IsChecked = true;
					break;
				case 4:
					access_timeMI.IsChecked = true;
					break;
				case 5:
					mod_timeMI.IsChecked = true;
					break;
			}
		}

		public void ColumnDisableManual(int index)
		{
			switch (index)
			{
				case 0:
					filenameMI.IsChecked = false;
					break;
				case 1:
					pathMI.IsChecked = false;
					break;
				case 2:
					sizeMI.IsChecked = false;
					break;
				case 3:
					creation_timeMI.IsChecked = false;
					break;
				case 4:
					access_timeMI.IsChecked = false;
					break;
				case 5:
					mod_timeMI.IsChecked = false;
					break;
			}
		}

		private void SyncEnableColumn(int index)
		{
			if (files)
			{
				foreach (var entry in Globals.filesFound)
				{
					if (entry != this)
					{
						entry.ColumnEnableManual(index);
					}
				}
			}
			else
			{
				foreach (var entry in Globals.dirsFound)
				{
					if (entry != this)
					{
						entry.ColumnEnableManual(index);
					}
				}
			}
		}

		private void SyncDisableColumn(int index)
		{
			if (files)
			{
				foreach (var entry in Globals.filesFound)
				{
					if (entry != this)
					{
						entry.ColumnDisableManual(index);
					}
				}
			}
			else
			{
				foreach (var entry in Globals.dirsFound)
				{
					if (entry != this)
					{
						entry.ColumnDisableManual(index);
					}
				}
			}
		}

		private void SyncColumnWidths(object sender, PropertyChangedEventArgs e, int index)
		{
			if (e.PropertyName == "ActualWidth")
			{
				double newWidth = ((GridViewColumn)sender).ActualWidth;
				if (files)
				{
					foreach (var entry in Globals.filesFound)
					{
						if (entry != this)
						{
							entry.ColumnReferences[index].Width = newWidth;
						}
					}

					fileResultsColumnWidths[index] = newWidth;
				}
				else
				{
					foreach (var entry in Globals.dirsFound)
					{
						if (entry != this)
						{
							entry.ColumnReferences[index].Width = newWidth;
						}
					}

					dirResultsColumnWidths[index] = newWidth;
				}
			}
		}

		private void ColumnWidthChanged0(object sender, PropertyChangedEventArgs e)
		{
			SyncColumnWidths(sender, e, 0);
		}

		private void ColumnWidthChanged1(object sender, PropertyChangedEventArgs e)
		{
			SyncColumnWidths(sender, e, 1);
		}

		private void ColumnWidthChanged2(object sender, PropertyChangedEventArgs e)
		{
			SyncColumnWidths(sender, e, 2);
		}

		private void ColumnWidthChanged3(object sender, PropertyChangedEventArgs e)
		{
			SyncColumnWidths(sender, e, 3);
		}

		private void ColumnWidthChanged4(object sender, PropertyChangedEventArgs e)
		{
			SyncColumnWidths(sender, e, 4);
		}

		private void ColumnWidthChanged5(object sender, PropertyChangedEventArgs e)
		{
			SyncColumnWidths(sender, e, 5);
		}

		private void Expander_OnExpanded(object sender, RoutedEventArgs e)
		{
			if (initialExpanson)
			{
				initialExpanson = false;
				return;
			}
			SelectCorrespondingCapture();
		}

		private void Expander_OnCollapsed(object sender, RoutedEventArgs e)
		{
			SelectCorrespondingCapture();
		}

		public void SelectCorrespondingCapture()
		{
			TreeViewItem trv = Globals.FindTviFromObjectRecursive(Globals.capturesTrv, searchResultCaptureView.Capture);
			if (trv != null)
			{
				trv.IsSelected = true;
			}
		}

		private void ResultsListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectCorrespondingCapture();
			e.Handled = true;
		}
	}
}
