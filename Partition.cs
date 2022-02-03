﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ColdStorageManager
{
	public class Partition
	{

		public ulong TotalSpace { get; set; }
		public ulong FreeSpace { get; set; }
		public string Letter { get; set; }
		public string Label { get; set; }
		public uint DiskIndex { get; set; }
		public uint Index { get; set; }

		public string FreeSpaceFormatted { get; set; }
		public string TotalSpaceFormatted { get; set; }

		public string UsedSpaceFormatted { get; set; }

		public ushort UsedSpacePercent { get; set; }
		public PhysicalDrive Parent { get; set; }

		public Partition(ulong totalSpace, ulong freeSpace, string letter, string label, uint diskIndex, uint index, PhysicalDrive parent)
		{
			TotalSpace = totalSpace;
			FreeSpace = freeSpace;
			Letter = letter;
			Label = label;
			DiskIndex = diskIndex;
			Index = index;
			FreeSpaceFormatted = Globals.GetFormattedSize(freeSpace);
			TotalSpaceFormatted = Globals.GetFormattedSize(totalSpace);
			UsedSpaceFormatted = FreeSpaceFormatted + "/" + TotalSpaceFormatted;
			UsedSpacePercent = (ushort)(100 - Math.Round(100F*freeSpace / totalSpace));
			Parent = parent;
			//Console.WriteLine(Letter + " " + UsedSpacePercent + "free: " + freeSpace + " total: " + totalSpace + " ratio: " + (double)freeSpace / totalSpace);
		}

		public override string ToString()
		{
			return $"{nameof(TotalSpace)}: {TotalSpace}, {nameof(FreeSpace)}: {FreeSpace}, {nameof(Letter)}: {Letter}, {nameof(Label)}: {Label}, {nameof(DiskIndex)}: {DiskIndex}, {nameof(Index)}: {Index}";
		}
	}
}
