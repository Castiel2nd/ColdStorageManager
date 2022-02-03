using System;
using System.Collections.Generic;
using System.Text;

namespace ColdStorageManager
{
	public class PhysicalDrive
	{
		public string Model { get; set; }
		public string SerialNumber { get; set; }
		public ulong TotalSpace { get; set; }
		public uint Index { get; set; }

		public List<Partition> Partitions { get; }

		public void SortPartitionsByIndex()
		{
			Partitions.Sort((Partition p1, Partition p2) => p1.Index.CompareTo(p2.Index));
		}

		public string GetFormattedSize { get; set; }

		public PhysicalDrive(string model, string serialNumber, ulong totalSpace, uint index)
		{
			this.Model = model.Trim();
			this.SerialNumber = serialNumber.Trim();
			this.TotalSpace = totalSpace;
			this.Index = index;
			GetFormattedSize = Globals.GetFormattedSize(totalSpace);
			Partitions = new List<Partition>();
		}

		public override string ToString()
		{
			return $"{nameof(Model)}: {Model}, {nameof(SerialNumber)}: {SerialNumber}, {nameof(TotalSpace)}: {TotalSpace}, {nameof(Index)}: {Index}";
		}
	}
}
