using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ColdStorageManager
{
	public abstract class CSMFileSystemEntry
	{
		public string Name { get; set; }
		public string Path { get; set; }

		public Boolean IsChecked { get; set; }

		public ImageSource Icon { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}

	public class CSMDirectory : CSMFileSystemEntry
	{
		private DirectoryInfo directoryInfo;
		private WpfObservableRangeCollection<CSMFileSystemEntry> children;

		public bool IsEmpty { get; set; }
		public bool IsExpanded { get; set; }
		public bool IsUnaccessible { get; set; }
		public List<CSMFileSystemEntry> ChildrenList { get; set; }

		public WpfObservableRangeCollection<CSMFileSystemEntry> Children
		{
			get
			{
				//Console.WriteLine("Children getter");
				return children;
			}
			set
			{
				children = value;
			}
		}
		public CSMDirectory(string path, bool getIcon = true)
		{
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
			IsExpanded = false;
			IsUnaccessible = false;
			IsChecked = true;
			if (getIcon)
			{
				Icon = Win32.ToImageSource(Win32.Extract(Path));
			}
			Children = new WpfObservableRangeCollection<CSMFileSystemEntry>();
			Children.Add(new CSMPlaceholder(Application.Current.Resources["loading"].ToString()));
			Name = directoryInfo.Name;
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
			ChildrenList = Globals.GetFileSystemEntries(Path, false);
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

		public string Size { get; set; }

		public CSMFile(string path, bool getIcon = true)
		{
			fileInfo = new FileInfo(path);
			Path = path;
			IsChecked = true;
			if (getIcon)
			{
				Icon = Win32.ToImageSource(Win32.Extract(Path));
			}
			Size = fileInfo.Length.ToString();
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
