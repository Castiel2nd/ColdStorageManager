using System;
using System.Collections.Generic;
using System.Text;

namespace ColdStorageManager.Models
{

	public class CaptureBase
	{
		public string drive_model { get; set; }
		public string drive_sn { get; set; }
		public string drive_nickname { get; set; }
		public ulong drive_size { get; set; }
	}

	public class Capture : CaptureBase
	{
		public int id { get; set; }
		public string partition_label { get; set; }
		public uint partition_number { get; set; }
		public ulong partition_size { get; set; }
		public ulong partition_free_space { get; set; }
		public string FreeSpaceFormatted { get; set; }
		public string TotalSpaceFormatted { get; set; }

		public string UsedSpaceFormatted { get; set; }

		public ushort UsedSpacePercent { get; set; }
		public string capture_datetime { get; set; }
		public DateTime capture_datetime_object { get; set; }

		public string capture_datetime_localized
		{
			get
			{
				return capture_datetime_object.ToString();
			}
		}

		public byte[] capture { get; set; }

		public Capture(int id, string driveModel, string driveSn, ulong driveSize, string driveNickname, string partitionLabel,
			uint partitionNumber, ulong partitionSize, ulong partitionFreeSpace, string captureDatetime, byte[] capture)
		{
			this.id = id;
			drive_model = driveModel;
			drive_sn = driveSn;
			drive_size = driveSize;
			drive_nickname = driveNickname;
			partition_label = partitionLabel;
			partition_number = partitionNumber;
			partition_size = partitionSize;
			partition_free_space = partitionFreeSpace;
			capture_datetime = captureDatetime;
			SetViews();
			this.capture = capture;
		}

		public Capture(string driveModel, string driveSn, ulong driveSize, string driveNickname, string partitionLabel,
			uint partitionNumber, ulong partitionSize, ulong partitionFreeSpace, string captureDatetime, byte[] capture)
		{
			this.id = -1;
			drive_model = driveModel;
			drive_sn = driveSn;
			drive_size = driveSize;
			drive_nickname = driveNickname;
			partition_label = partitionLabel;
			partition_number =partitionNumber;
			partition_size = partitionSize;
			partition_free_space = partitionFreeSpace;
			capture_datetime = captureDatetime;
			SetViews();
			this.capture = capture;
		}

		public void SetViews()
		{
			FreeSpaceFormatted = Globals.GetFormattedSize(partition_free_space);
			TotalSpaceFormatted = Globals.GetFormattedSize(partition_size);
			UsedSpaceFormatted = FreeSpaceFormatted + "/" + TotalSpaceFormatted;
			UsedSpacePercent = (ushort)(100 - Math.Round(100F * partition_free_space / partition_size));
			capture_datetime_object = DateTime.Parse(capture_datetime);
		}

		public Capture()
		{

		}
	}

	public class CapturePhDisk : CaptureBase
	{

		public List<Capture> captures { get; set; }

		public string FormattedSize { get; set; }

		public CapturePhDisk(string driveModel, string driveSn, ulong driveSize, string driveNickname)
		{
			drive_model = driveModel;
			drive_sn = driveSn;
			drive_size = driveSize;
			FormattedSize = Globals.GetFormattedSize(driveSize);
			drive_nickname = driveNickname;
			captures = new List<Capture>();
		}
	}

	public class CapturePlaceholder : CaptureBase
	{
		public string Msg { get; set; }

		public CapturePlaceholder(string msg)
		{
			Msg = msg;
		}
	}
}
