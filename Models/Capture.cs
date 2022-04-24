using System;
using System.Collections.Generic;
using System.Text;

namespace ColdStorageManager.Models
{

	public class CaptureBase
	{
		public string drive_model { get; set; }
		
		public string drive_sn { get; set; }
		
		public bool isNVMe { get; set; }
		
		public bool NVMeSerialNumberDetectionFail { get; set; }
		
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
		
		public string volume_guid { get; set; }
		
		public string FreeSpaceFormatted { get; set; }
		
		public string TotalSpaceFormatted { get; set; }
		
		public string UsedSpaceFormatted { get; set; }
		
		public ushort UsedSpacePercent { get; set; }
		
		public ushort capture_properties { get; set; }
		
		public string capture_datetime { get; set; }
		
		public DateTime capture_datetime_object { get; set; }

		public uint capture_lines_number { get; set; }

		public string capture_datetime_localized
		{
			get
			{
				return capture_datetime_object.ToString();
			}
		}

		public uint capture_files_number { get; set; }
		
		public uint capture_directories_number { get; set; }
		
		public byte[] capture { get; set; }
		
		public byte[] sizes { get; set; }
		
		public long[] sizes_prepared { get; set; }
		
		public byte[] creation_times { get; set; }
		
		public DateTime[] creation_times_prepared { get; set; }
		
		public byte[] last_access_times { get; set; }
		
		public DateTime[] last_access_times_prepared { get; set; }
		
		public byte[] last_mod_times { get; set; }
		
		public DateTime[] last_mod_times_prepared { get; set; }



		public Capture(int id, string driveModel, string driveSn, bool isNVMe, bool NVMeSerialNumberDetectionFail, ulong driveSize, string driveNickname, string partitionLabel,
			uint partitionNumber, ulong partitionSize, ulong partitionFreeSpace, string volumeGUID, ushort captureProperties, string captureDatetime,
			uint captureLinesNumber, uint captureFilesNumber, uint captureDirectoriesNumber,
			byte[] capture, byte[] sizes = default, byte[] creation_times = default, byte[] last_access_times = default, byte[] last_mod_times = default)
		{
			this.id = id;
			drive_model = driveModel;
			drive_sn = driveSn;
			this.isNVMe = isNVMe;
			this.NVMeSerialNumberDetectionFail = NVMeSerialNumberDetectionFail;
			drive_size = driveSize;
			drive_nickname = driveNickname;
			partition_label = partitionLabel;
			partition_number = partitionNumber;
			partition_size = partitionSize;
			partition_free_space = partitionFreeSpace;
			volume_guid = volumeGUID;
			capture_properties = captureProperties;
			capture_datetime = captureDatetime;
			capture_lines_number = captureLinesNumber;
			SetViews();
			capture_files_number = captureFilesNumber;
			capture_directories_number = captureDirectoriesNumber;
			this.capture = capture;
			this.sizes = sizes;
			this.creation_times = creation_times;
			this.last_access_times = last_access_times;
			this.last_mod_times = last_mod_times;
		}

		public Capture(string driveModel, string driveSn, bool isNVMe, bool NVMeSerialNumberDetectionFail, ulong driveSize, string driveNickname, string partitionLabel,
			uint partitionNumber, ulong partitionSize, ulong partitionFreeSpace, string volumeGUID, ushort captureProperties, string captureDatetime,
			uint captureLinesNumber, uint captureFilesNumber, uint captureDirectoriesNumber,
			byte[] capture, byte[] sizes = default, byte[] creation_times = default, byte[] last_access_times = default, byte[] last_mod_times = default)
		{
			this.id = -1;
			drive_model = driveModel;
			drive_sn = driveSn;
			this.isNVMe = isNVMe;
			this.NVMeSerialNumberDetectionFail = NVMeSerialNumberDetectionFail;
			drive_size = driveSize;
			drive_nickname = driveNickname;
			partition_label = partitionLabel;
			partition_number =partitionNumber;
			partition_size = partitionSize;
			partition_free_space = partitionFreeSpace;
			volume_guid = volumeGUID;
			capture_properties = captureProperties;
			capture_datetime = captureDatetime;
			capture_lines_number = captureLinesNumber;
			SetViews();
			capture_files_number = captureFilesNumber;
			capture_directories_number = captureDirectoriesNumber;
			this.capture = capture;
			this.sizes = sizes;
			this.creation_times = creation_times;
			this.last_access_times = last_access_times;
			this.last_mod_times = last_mod_times;
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
