using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
	/// Interaction logic for LogPanel.xaml
	/// </summary>
	public partial class LogPanel : UserControl
	{

		private const int MaxSize = 5000;
		private const int MinSize = 50;
		//false == reverse direction
		bool verticalDir, horizontalDir;

		public delegate void RefreshPosition();
		RefreshPosition refreshPosition;

		public LogPanel()
		{
			InitializeComponent();
		}

		public void SetRefresher(RefreshPosition refresher)
		{
			refreshPosition = refresher;
		}

		public void SetBorder(bool isWestBorderEnabled, bool isNorthBorderEnabled, bool isEastBorderEnabled)
		{
			if (!isWestBorderEnabled)
			{
				mainGrid.Children.Remove(westThumb);
				mainGrid.Children.Remove(northWestThumb);
			}

			if (!isNorthBorderEnabled)
			{
				mainGrid.Children.Remove(northThumb);
				mainGrid.Children.Remove(northWestThumb);
				mainGrid.Children.Remove(northEastThumb);
			}

			if (!isEastBorderEnabled)
			{
				mainGrid.Children.Remove(eastThumb);
				mainGrid.Children.Remove(northEastThumb);
			}
		}

		public void BindDisplayData(Binding binding)
		{
			displayTxtBox.SetBinding(TextBox.TextProperty, binding);
		}

		private void onDragStarted(object sender, DragStartedEventArgs e)

		{
			Thumb t = (Thumb)sender;
		}

		private void onDragDelta(object sender, DragDeltaEventArgs e)

		{
			Thumb t = sender as Thumb;

			if (t == westThumb)
			{
				horizontalDir = false;
			}
			else if (t == northWestThumb)
			{
				horizontalDir = false;
				verticalDir = false;
			}else if(t == northThumb)
			{
				verticalDir = false;
			}else if (t == northEastThumb)
			{
				horizontalDir = true;
				verticalDir = false;
			}
			else
			{
				horizontalDir = true;
			}

			if (t.Cursor == Cursors.SizeWE
			    || t.Cursor == Cursors.SizeNWSE || t.Cursor == Cursors.SizeNESW)
			{
				// Console.WriteLine("ActualWidth: " + ActualWidth);
				// Console.WriteLine("HorizontalChange: " + e.HorizontalChange);

				Width = Math.Min(MaxSize,
					Math.Max(Width + (horizontalDir ? e.HorizontalChange : -e.HorizontalChange),
						MinSize));

				refreshPosition();
			}

			if (t.Cursor == Cursors.SizeNS
			    || t.Cursor == Cursors.SizeNWSE || t.Cursor == Cursors.SizeNESW)
			{
				// Console.WriteLine("ActualHeight: " + ActualHeight);
				// Console.WriteLine("VerticalChange: " + e.VerticalChange);

				Height = Math.Min(MaxSize,
					Math.Max(Height + (verticalDir ? e.VerticalChange : -e.VerticalChange),
						MinSize));

				refreshPosition();
			}
		}

		private void onDragCompleted(object sender, DragCompletedEventArgs e)

		{
			Thumb t = (Thumb)sender;
		}
    }
}
