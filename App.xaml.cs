using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Management;
using System.Text.RegularExpressions;

namespace ColdStorageManager
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{

		private void App_OnStartup(object sender, StartupEventArgs e)
		{
			GetDriveInfo();
			new MainWindow().Show();
		}

		//gets info from WMI/CIM using the System.Management class
		private void GetDriveInfo()
		{
			ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
			if (Globals.physicalDrives != null)
			{
				Globals.physicalDrives.Clear();
			}
			else
			{
				Globals.physicalDrives = new List<PhysicalDrive>();
			}
			foreach (ManagementBaseObject baseObject in searcher.Get())
			{
				if (baseObject != null)
				{
					PhysicalDrive physicalDrive = new PhysicalDrive(
						(string)baseObject["Model"],
						(string)baseObject["SerialNumber"],
						(ulong)baseObject["Size"],
						(uint)baseObject["Index"]
					);
					//Console.WriteLine(physicalDrive);
					Globals.physicalDrives.Add(physicalDrive);
				}
			}
			//sort by index
			Globals.physicalDrives.Sort((PhysicalDrive pd1, PhysicalDrive pd2) => pd1.Index.CompareTo(pd2.Index));
			//foreach (var VARIABLE in Globals.physicalDrives)
			//{
			//	Console.WriteLine(VARIABLE);
			//}

			//querying for info to match logical drives to physical disks
			//this and most things onwards should probably be done via System.IO.DriveInfo class
			List<(uint DiskIndex, uint PartIndex, string Letter)> LogicalToPartition =
				new List<(uint DiskIndex, uint PartIndex, string Letter)>();

			searcher.Query = new ObjectQuery("SELECT * FROM CIM_LogicalDiskBasedOnPartition");

			foreach (ManagementBaseObject baseObject in searcher.Get())
			{
				if (baseObject != null)
				{
					string diskPartition = Regex.Match((string)baseObject["Antecedent"], @"Disk #\d+, Partition #\d+")
						.Value;
					uint diskIndex, partIndex;
					var ints = Regex.Matches(diskPartition, @"\d+");
					diskIndex = uint.Parse(ints[0].Value);
					partIndex = uint.Parse(ints[1].Value);
					string dependent = (string)baseObject["Dependent"];
					string letter = dependent.Substring(dependent.Length - 3, 2);
					LogicalToPartition.Add((diskIndex, partIndex, letter));
				}
			}

			//getting the logical disks and creating Partitions
			searcher.Query = new ObjectQuery("SELECT * FROM CIM_LogicalDisk");

			foreach (ManagementBaseObject baseObject in searcher.Get())
			{
				if (baseObject != null)
				{
					string letter = (string)baseObject["Name"];
					foreach (var tuple in LogicalToPartition)
					{
						if (letter == tuple.Letter)
						{
							foreach (var phDisk in Globals.physicalDrives)
							{
								if (phDisk.Index == tuple.DiskIndex)
								{
									phDisk.Partitions.Add(
										new Partition(
											(ulong)baseObject["Size"],
											(ulong)baseObject["FreeSpace"],
											letter,
											null,
											tuple.DiskIndex,
											tuple.PartIndex,
											phDisk
											)
										);
								}
							}
						}
					}
				}
			}

			DriveInfo[] drives = DriveInfo.GetDrives();
			foreach (var drive in drives)
			{
				foreach (var physicalDrive in Globals.physicalDrives)
				{
					foreach (var partition in physicalDrive.Partitions)
					{
						if (partition.Letter[0] == drive.Name[0])
						{
							partition.Label = drive.VolumeLabel;
						}
					}
				}
			}

			//sorting partitions by index for each disk
			foreach (var physicalDrive in Globals.physicalDrives)
			{
				physicalDrive.SortPartitionsByIndex();
			}
		}
		
	}
}
