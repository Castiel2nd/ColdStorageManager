using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ColdStorageManager.Annotations;
using static ColdStorageManager.Globals;

namespace ColdStorageManager
{
	public abstract class CSMFileSystemEntry : INotifyPropertyChanged
	{
		private static ImageSource iconImageSource;

		protected bool? isChecked;

		public string Name { get; set; }
		public string Path { get; set; }

		public CSMDirectory Parent { get; set; }

		public virtual bool? IsChecked
		{
			get
			{
				return isChecked;
			}
			set
			{
				isChecked = value;
				//prevent the propagation of the setting of the 3rd state
				if (Parent != null && value != null)
				{
					Parent.ChildChecked(value);
				}
				IsCheckedChanged();
			}
		}

		private ImageSource _icon = null;

		public ImageSource Icon
		{
			get
			{
				if (_icon == null)
				{
					SerialTaskFactory.StartNew(() =>
					{
						iconImageSource = Win32.ToImageSource(Win32.Extract(Path));
						iconImageSource.Freeze();
						Icon = iconImageSource;
					});

					// Application.Current.Dispatcher.InvokeAsync(async () => {Icon = await GetIconFromPathAsync(Path); Console.WriteLine("hello");});

					// Task.Run(() =>
					// {
					// 	Console.WriteLine($"Icon for {Path}:");
					// 	stopwatch.Reset();
					// 	stopwatch.Start();
					// 	var icon = Win32.Extract(Path);
					// 	Thread.Sleep(50);
					// 	stopwatch.Stop();
					//
					// 	long ms1 = stopwatch.ElapsedMilliseconds;
					//
					// 	stopwatch.Reset();
					// 	stopwatch.Start();
					// 	var iconImageSource = Win32.ToImageSource(icon);
					// 	iconImageSource.Freeze();
					// 	Icon = iconImageSource;
					//
					// 	stopwatch.Stop();
					//
					// 	long ms2 = stopwatch.ElapsedMilliseconds;
					//
					// 	Console.WriteLine($"Time for Icon-extraction: {ms1}, time for icon-conversion: {ms2}");
					// });
				}

				return _icon;
			}

			set
			{
				if (_icon != value)
				{
					_icon = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));

					// Console.WriteLine($"NULL? {(value == null ? "YES" : "NO")}");
					// Console.WriteLine($"{value.GetType().FullName} height: {value.Height}");
					// Console.WriteLine("PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));");
					
				}
			}
		}

		public virtual void CheckedByParent(bool? value)
		{
			isChecked = value;
			IsCheckedChanged();
		}

		public async Task<ImageSource> GetIconFromPathAsync(string path)
		{
			return await Task.Run(() => Win32.ToImageSource(Win32.Extract(path)));
		}

		protected void IsCheckedChanged()
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
		}

		public void DirectCheck(bool? value)
		{
			isChecked = value;
		}

		public override string ToString()
		{
			return Name;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class CSMDirectory : CSMFileSystemEntry
	{
		private DirectoryInfo directoryInfo;
		private WpfObservableRangeCollection<CSMFileSystemEntry> children;
		private int checkedCount = 0;
		private bool isExpanded = false;

		public bool IsEmpty { get; set; }

		public bool IsExpanded
		{
			get
			{
				return isExpanded;
			}
			set
			{
				if (value)
				{
					checkedCount = children.Count;
				}

				isExpanded = value;
			}
		}

		public override bool? IsChecked
		{
			get
			{
				return isChecked;
			}
			set
			{
				//prevent the propagation of the setting of the 3rd state
				if (value == null)
				{
					isChecked = null;
					IsCheckedChanged();
					return;
				}
				isChecked = value;
				IsCheckedChanged();
				Parent?.ChildChecked(value);
				CheckChildren(value);
			}
		}

		public bool IsUnaccessible { get; set; }
		public List<CSMFileSystemEntry> ChildrenList { get; set; }

		public WpfObservableRangeCollection<CSMFileSystemEntry> Children
		{
			get
			{
				return children;
			}
			set
			{
				children = value;
			}
		}

		public DateTime GetCreationTime => directoryInfo.CreationTime;
		public DateTime GetLastAccessTime => directoryInfo.LastAccessTime;
		public DateTime GetLastModificationTime => directoryInfo.LastWriteTime;

		private bool toPrint { get; set; }

		// public static async Task<CSMDirectory> CreateDirectoryAsync(string path, CSMDirectory parent = null,
		// 	bool getIcon = true)
		// {
		// 	var dir = new CSMDirectory(path, parent, getIcon);
		// }

		public CSMDirectory(string path, CSMDirectory parent = null)
		{
			Path = path;
			Parent = parent;
			IsUnaccessible = false;
			directoryInfo = new DirectoryInfo(path);
			Name = directoryInfo.Name;

			/*try
			{
				IsEmpty = !directoryInfo.EnumerateFileSystemInfos().Any();
			}
			catch (UnauthorizedAccessException e)
			{
				IsEmpty = true;
				IsUnaccessible = true;
			}*/

			IsEmpty = false;

			if (Parent != null)
			{
				isChecked = Parent.IsChecked;
			}
			else
			{
				isChecked = true;
			}

			// is this an exception?
			if (Globals.ExceptionPathsEnable && isChecked == true)
			{
				foreach (var exceptionPath in Globals.exceptionPathsOC)
				{
					if ((exceptionPath.Length == (path.Length - 2)) && path.EndsWith(exceptionPath))
					{
						isChecked = false;
						break;
					}
				}
			}

			children = new WpfObservableRangeCollection<CSMFileSystemEntry>();
			children.Add(new CSMPlaceholder(Application.Current.Resources["loading"].ToString()));
		}

		public override void CheckedByParent(bool? value)
		{
			base.CheckedByParent(value);
			CheckChildren(value);
		}

		private void CheckChildren(bool? value)
		{
			if (isExpanded)
			{
				foreach (var csmFileSystemEntry in children)
				{
					csmFileSystemEntry.CheckedByParent(value);
				}
			}
		}

		public void ChildChecked(bool? value)
		{
			if(value == true)
			{
				checkedCount++;
				if (checkedCount == children.Count)
				{
					isChecked = true;
				}
				else
				{
					isChecked = null;
				}
			}
			else
			{
				checkedCount--;
				if (checkedCount == 0)
				{
					isChecked = false;
				}
				else
				{
					isChecked = null;
				}

			}
			IsCheckedChanged();
			Parent?.ChildChecked(isChecked);
		}

		public void ConvertChildrenToListPropagate()
		{
			IsExpanded = false;
			ChildrenList = children.ToList();
			foreach (var entry in ChildrenList)
			{
				if (entry is CSMDirectory dir)
				{
					dir.ConvertChildrenToListPropagate();
				}
			}
		}

		public void ExpandChildrenListPropagate()
		{
			ChildrenList = Globals.GetFileSystemEntries(Path, this);
			IsExpanded = true;
			if (ChildrenList.Count == 0)
			{
				IsEmpty = true;
			}
			else
			{
				foreach (var entry in ChildrenList)
				{
					if (entry is CSMDirectory dir)
					{
						if (!dir.IsExpanded)
						{
							dir.ExpandChildrenListPropagate();
						}
					}
				}
			}
		}
	}

	public class CSMFile : CSMFileSystemEntry
	{
		private FileInfo fileInfo;

		public long Size { get; }

		public DateTime GetCreationTime => fileInfo.CreationTime;
		public DateTime GetLastAccessTime => fileInfo.LastAccessTime;
		public DateTime GetLastModificationTime => fileInfo.LastWriteTime;

		//Dummy property to prevent binding failures while scrolling fast in the file dialog TreeView. Reason unknown. Greatly improves performance(at least in debug mode).
		public WpfObservableRangeCollection<CSMFileSystemEntry> Children
		{
			get
			{
				return null;
			}
		}

		public CSMFile(string path, CSMDirectory parent = null)
		{
			Path = path;
			Parent = parent;
			fileInfo = new FileInfo(path);
			Size = fileInfo.Length;
			Name = fileInfo.Name;

			if (Parent != null)
			{
				isChecked = Parent.IsChecked;

			}
			else
			{
				isChecked = true;
			}

			// is this an exception?
			if (Globals.ExceptionPathsEnable && isChecked == true)
			{
				foreach (var exceptionPath in Globals.exceptionPathsOC)
				{
					if ((exceptionPath.Length == (path.Length - 2)) && path.EndsWith(exceptionPath))
					{
						isChecked = false;
						break;
					}
				}

				if (Globals.ExceptionFileTypesCaptureEnable && isChecked == true)
				{
					foreach (var exceptionFileType in Globals.exceptionFileTypesCaptureList)
					{
						if (Name.EndsWith(exceptionFileType))
						{
							isChecked = false;
							break;
						}
					}
				}
			}
		}
	}

	public class CSMPlaceholder : CSMFileSystemEntry
	{
		public CSMPlaceholder(string name)
		{
			Name = name;
		}
	}
}
