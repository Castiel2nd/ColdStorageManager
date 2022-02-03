using System;
using System.Collections.Generic;
using System.Text;

namespace ColdStorageManager
{
	//Customized List implementation that returns a Count>0 if 
	//the parent directory has children, but they have not actually been listed yet.
	//This is for the on-demand treeView filesystem panel.
	public class CSMFileSystemList<T> : List<T>
	{
		private bool isParentEmpty;

		public CSMFileSystemList(bool isParentEmpty) : base()
		{
			this.isParentEmpty = isParentEmpty;
		}

		public new int Count
		{
			get
			{
				Console.WriteLine("CSMFileSystemList.Count getter");
				if (isParentEmpty)
				{
					return 0;
				}
				else
				{
					return base.Count > 0 ? base.Count : 1;
				}
			}
		} 

	}
}
