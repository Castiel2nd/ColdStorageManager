using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ColdStorageManager.Annotations;

namespace ColdStorageManager
{
	public abstract class CSMFileSystemEntry : INotifyPropertyChanged
	{
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
				if (Parent != null)
				{
					Parent.ChildChecked(value);
				}
				IsCheckedChanged();
			}
		}

		public ImageSource Icon { get; set; }

		public virtual void CheckedByParent(bool? value)
		{
			isChecked = value;
			IsCheckedChanged();
		}

		protected void IsCheckedChanged()
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsChecked"));
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

		public CSMDirectory(string path, CSMDirectory parent = null, bool getIcon = true)
		{
			Parent = parent;
			IsUnaccessible = false;
			directoryInfo = new DirectoryInfo(path);

			/*try
			{
				IsEmpty = !directoryInfo.EnumerateFileSystemInfos().Any();
			}
			catch (UnauthorizedAccessException e)
			{
				IsEmpty = true;
				IsUnaccessible = true;
			}*/

			Path = path;
			IsEmpty = false;
			IsUnaccessible = false;
			if (Parent != null)
			{
				isChecked = Parent.IsChecked;
			}
			else
			{
				isChecked = true;
			}
			if (getIcon)
			{
				Icon = Win32.ToImageSource(Win32.Extract(Path));
			}
			children = new WpfObservableRangeCollection<CSMFileSystemEntry>();
			children.Add(new CSMPlaceholder(Application.Current.Resources["loading"].ToString()));
			Name = directoryInfo.Name;
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
			if (value == null)
			{
				isChecked = null;
			}
			else if(value == true)
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
			ChildrenList = Globals.GetFileSystemEntries(Path, this, false);
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

		public CSMFile(string path, CSMDirectory parent = null, bool getIcon = true)
		{
			Parent = parent;
			fileInfo = new FileInfo(path);
			Path = path;
			if (Parent != null)
			{
				isChecked = Parent.IsChecked;
			}
			else
			{
				isChecked = true;
			}
			if (getIcon)
			{
				Icon = Win32.ToImageSource(Win32.Extract(Path));
			}

			Size = fileInfo.Length;
			Name = fileInfo.Name;
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
