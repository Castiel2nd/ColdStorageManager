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
		
		public bool preventLoopback;

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

			columns.CollectionChanged += SyncColumnMoves;
			((INotifyPropertyChanged)columnReferences[0]).PropertyChanged += ColumnWidthChanged0;
			((INotifyPropertyChanged)columnReferences[1]).PropertyChanged += ColumnWidthChanged1;
			((INotifyPropertyChanged)columnReferences[2]).PropertyChanged += ColumnWidthChanged2;
			((INotifyPropertyChanged)columnReferences[3]).PropertyChanged += ColumnWidthChanged3;
			((INotifyPropertyChanged)columnReferences[4]).PropertyChanged += ColumnWidthChanged4;
			((INotifyPropertyChanged)columnReferences[5]).PropertyChanged += ColumnWidthChanged5;
		}

		public void SetColumnWidths(double first, double second, double third, double fourth, double fifth,
			double sixth)
		{
			columnReferences[0].Width = first;
			columnReferences[1].Width = second;
			columnReferences[2].Width = third;
			columnReferences[3].Width = fourth;
			columnReferences[4].Width = fifth;
			columnReferences[5].Width = sixth;
		}

		private void InsertColumn(int index)
		{
			if (columns?.Count <= index)
			{
				columns.Add(columnReferences[index]);
			}
			else
			{
				columns?.Insert(index, columnReferences[index]);
			}
		}

		private void fileNameMenuItem_OnChecked(object sender, RoutedEventArgs e)
		{
			InsertColumn(0);
			SyncEnableColumn(0);
		}

		private void fileNameMenuItem_OnUnchecked(object sender, RoutedEventArgs e)
		{
			columns.Remove(columnReferences[0]);
			SyncDisableColumn(0);
		}

		private void pathMenuItem_OnChecked(object sender, RoutedEventArgs e)
		{
			InsertColumn(1);
			SyncEnableColumn(1);
		}

		private void pathMenuItem_OnUnchecked(object sender, RoutedEventArgs e)
		{
			columns.Remove(columnReferences[1]);
			SyncDisableColumn(1);
		}

		private void sizeMenuItem_OnChecked(object sender, RoutedEventArgs e)
		{
			InsertColumn(2);
			SyncEnableColumn(2);
		}

		private void sizeMenuItem_OnUnchecked(object sender, RoutedEventArgs e)
		{
			columns.Remove(columnReferences[2]);
			SyncDisableColumn(2);
		}

		private void creation_timeMenuItem_OnChecked(object sender, RoutedEventArgs e)
		{
			InsertColumn(3);
			SyncEnableColumn(3);
		}

		private void creation_timeMenuItem_OnUnchecked(object sender, RoutedEventArgs e)
		{
			columns.Remove(columnReferences[3]);
			SyncDisableColumn(3);
		}

		private void access_timeMenuItem_OnChecked(object sender, RoutedEventArgs e)
		{
			InsertColumn(4);
			SyncEnableColumn(4);
		}

		private void access_timeMenuItem_OnUnchecked(object sender, RoutedEventArgs e)
		{
			columns.Remove(columnReferences[4]);
			SyncDisableColumn(4);
		}

		private void mod_timeMenuItem_OnChecked(object sender, RoutedEventArgs e)
		{
			InsertColumn(5);
			SyncEnableColumn(5);
		}

		private void mod_timeMenuItem_OnUnchecked(object sender, RoutedEventArgs e)
		{
			columns.Remove(columnReferences[5]);
			SyncDisableColumn(5);
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
				if (files)
				{
					foreach (var entry in Globals.filesFound)
					{
						if (entry != this)
						{
							entry.ColumnReferences[index].Width = ((GridViewColumn)sender).ActualWidth;
						}
					}
				}
				else
				{
					foreach (var entry in Globals.dirsFound)
					{
						if (entry != this)
						{
							entry.ColumnReferences[index].Width = ((GridViewColumn)sender).ActualWidth;
						}
					}
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
