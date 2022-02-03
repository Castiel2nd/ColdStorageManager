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
	}

	public class Capture : CaptureBase
	{
		public int id { get; set; }
		public uint partition_number { get; set; }
		public string capture_date { get; set; }
		public byte[] capture { get; set; }

		public Capture(int id, string driveModel, string driveSn, string driveNickname, uint partitionNumber, string captureDate, byte[] capture)
		{
			this.id = id;
			drive_model = driveModel;
			drive_sn = driveSn;
			drive_nickname = driveNickname;
			partition_number = partitionNumber;
			capture_date = captureDate;
			this.capture = capture;
		}

		public Capture(string driveModel, string driveSn, string driveNickname, uint partitionNumber, string captureDate, byte[] capture)
		{
			this.id = -1;
			drive_model = driveModel;
			drive_sn = driveSn;
			drive_nickname = driveNickname;
			partition_number =partitionNumber;
			capture_date = captureDate;
			this.capture = capture;
		}

		public Capture()
		{

		}
	}

	public class CapturePhDisk : CaptureBase
	{

		public List<Capture> captures { get; set; }

		public CapturePhDisk(string driveModel, string driveSn, string driveNickname)
		{
			drive_model = driveModel;
			drive_sn = driveSn;
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
