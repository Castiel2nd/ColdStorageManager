using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Media;
using ColdStorageManager.Models;

namespace ColdStorageManager
{
	internal static class CFSHandler
	{
		private const string fileExtension = ".cfs";
		private const string fileTypeIdentifier = "Cold_Storage_Manager-Captured_File_System";
		private const ushort numBlobTypes = Globals.numBlobTypes;
		private static string version = Globals.version;
		private const char dirEnterIndicator = '\\';
		private const char dirLeaveIndicator = '/';
		private const char dataSeparator = '|';
		private static StringBuilder stringBuilder;

		public static bool WriteCFSToFile(string fileName, string driveModel, string nickname)
		{
			using (MemoryStream ms = new MemoryStream(1000))
			{
				StreamWriter[] swArray = new StreamWriter[numBlobTypes];
				swArray[0] = new StreamWriter(ms);
				WriteToStreams(swArray, driveModel, nickname);

				using (FileStream fs = new FileStream(fileName + fileExtension, FileMode.Create, FileAccess.Write))
				{
					ms.WriteTo(fs);
				}

				foreach (var sw in swArray)
				{
					sw?.Close();
				}
			}

			using (FileStream fs = new FileStream(fileName + fileExtension, FileMode.Open, FileAccess.Read))
			using (StreamReader sr = new StreamReader(fs))
			{
				string content = sr.ReadToEnd();
			}

			return true;
		}

		public static (byte[] capture, byte[] sizes, byte[] creation_times, byte[] last_access_times, byte[] last_mod_times, uint lines)
			GetCFSBytes(string driveModel, string nickname, bool size = false, bool createTime  = false,
			bool accessTime = false, bool modTime = false)
		{
			(byte[] capture, byte[] sizes, byte[] creation_times, byte[] last_access_times, byte[] last_mod_times, uint lines) ret;

			MemoryStream[] msArray = new MemoryStream[numBlobTypes];
			StreamWriter[] swArray = new StreamWriter[numBlobTypes];

			//init arrays
			msArray[0] = new MemoryStream(1000);
			if(size)
				msArray[1] = new MemoryStream(1000);
			if (createTime)
				msArray[2] = new MemoryStream(1000);
			if (accessTime)
				msArray[3] = new MemoryStream(1000);
			if (modTime)
				msArray[4] = new MemoryStream(1000);
			swArray[0] = new StreamWriter(msArray[0]);
			if (size)
				swArray[1] = new StreamWriter(msArray[1]);
			if (createTime)
				swArray[2] = new StreamWriter(msArray[2]);
			if (accessTime)
				swArray[3] = new StreamWriter(msArray[3]);
			if (modTime)
				swArray[4] = new StreamWriter(msArray[4]);

			ret.lines = WriteToStreams(swArray, driveModel, nickname, size, createTime, accessTime, modTime);
			ret.capture = msArray[0].ToArray();
			ret.sizes = msArray[1]?.ToArray();
			ret.creation_times = msArray[2]?.ToArray();
			ret.last_access_times = msArray[3]?.ToArray();
			ret.last_mod_times = msArray[4]?.ToArray();

			for (int i = 0; i< numBlobTypes; i++)
			{
				swArray[i]?.Close();
				msArray[i]?.Close();
			}

			return ret;
		}

		public static void PrepareCapture(Capture capture)
		{
			if (capture.capture_properties != 0)
			{
				var bools = Globals.DecodeCaptureProperties(capture.capture_properties);
				bool size = bools.size;
				bool createTime = bools.createTime;
				bool lastAccessTime = bools.lastAccessTime;
				bool last = bools.lastModTime;
				uint lines = 0;
				string line;
				string[] splitData;
				using MemoryStream ms = new MemoryStream(capture.capture);
				using (StreamReader sr = new StreamReader(ms))
				{
					sr.ReadLine();
					sr.ReadLine();
					sr.ReadLine();
					
					while (!sr.EndOfStream)
					{
						line = sr.ReadLine();

						if (!line.EndsWith(dirLeaveIndicator))
						{

						}

						lines++;
					}
				}
			}
		}

		private static uint WriteToStreams(StreamWriter[] swArray, string driveModel, string nickname, bool size = false, bool createTime = false,
			bool accessTime = false, bool modTime = false)
		{
			List<CSMFileSystemEntry> list = Globals.fsList;
			//header for each file
			foreach (var sw in swArray)
			{
				sw?.WriteLine(fileTypeIdentifier + "#" + version);
				sw?.WriteLine(driveModel);
				sw?.WriteLine(nickname);
			}
			List<List<CSMFileSystemEntry>> listRefs =
				new List<List<CSMFileSystemEntry>>();
			List<int> indexes = new List<int>();
			int j = 0;
			listRefs.Add(list); indexes.Add(0);
			bool breaking = false;
			uint lines = 3;
			string entryStr;
			CSMFile file;
			for (int i = 0; ; lines++)
			{
				while (i == listRefs[j].Count)
				{
					if (j == 0)
					{
						breaking = true;
						break;
					}
					//write the leave indicator to every file
					foreach (var sw in swArray)
					{
						sw?.WriteLine(dirLeaveIndicator);
					}
					lines++;
					listRefs.RemoveAt(j);
					indexes.RemoveAt(j);
					j--;
					i = indexes[j];
				}
				if (breaking)
				{
					break;
				}

				CSMFileSystemEntry entry = listRefs[j][i];
				//Console.WriteLine("Written to sw: " + entry.Name);
				i++;
				if (entry is CSMDirectory dir)
				{
					swArray[0].WriteLine(entry.Name + dirEnterIndicator);
					swArray[1]?.WriteLine();
					if (createTime)
					{
						swArray[2].WriteLine(dir.GetCreationTime.ToString());
					}
					if (accessTime)
					{
						swArray[3].WriteLine(dir.GetLastAccessTime.ToString());
					}
					if (modTime)
					{
						swArray[4].WriteLine(dir.GetLastModificationTime.ToString());
					}
					if (dir.IsEmpty)
					{
						//write the leave indicator to every file
						foreach (var sw in swArray)
						{
							sw?.WriteLine(dirLeaveIndicator);
						}
						lines++;
					}
					else
					{
						indexes[j] = i;
						j++;
						listRefs.Add(dir.ChildrenList);
						indexes.Add(0);
						i = 0;
					}
				}
				else
				{
					file = entry as CSMFile;
					swArray[0].WriteLine(entry.Name);
					if (size)
					{
						swArray[1].WriteLine(file.Size.ToString());
					}
					if (createTime)
					{
						swArray[2].WriteLine(file.GetCreationTime.ToString());
					}
					if (accessTime)
					{
						swArray[3].WriteLine(file.GetLastAccessTime.ToString());
					}
					if (modTime)
					{
						swArray[4].WriteLine(file.GetLastModificationTime.ToString());
					}
				}
			}
			foreach (var sw in swArray)
			{
				sw?.Flush();
			}

			return lines;
		}

		private static void StrBldrRemoveLastDir()
		{
			stringBuilder.Length--;
			stringBuilder.Length = LastIndexOf(stringBuilder, dirEnterIndicator)+1;
		}

		public static int LastIndexOf(this StringBuilder sb, char find, bool ignoreCase = false, int startIndex = -1, CultureInfo culture = null)
		{
			if (sb == null) throw new ArgumentNullException(nameof(sb));
			if (startIndex == -1) startIndex = sb.Length - 1;
			if (startIndex < 0 || startIndex >= sb.Length) throw new ArgumentException("startIndex must be between 0 and sb.Lengh-1", nameof(sb));
			if (culture == null) culture = CultureInfo.InvariantCulture;

			int lastIndex = -1;
			if (ignoreCase) find = Char.ToUpper(find, culture);
			for (int i = startIndex; i >= 0; i--)
			{
				char c = ignoreCase ? Char.ToUpper(sb[i], culture) : (sb[i]);
				if (find == c)
				{
					lastIndex = i;
					break;
				}
			}
			return lastIndex;
		}

		public static (List<string> files, List<string> dirs) Search(byte[] capture, string stringToSearch)
		{
			List<string> files = new List<string>();
			List<string> dirs = new List<string>();

			if (stringBuilder == null)
			{
				stringBuilder = new StringBuilder();
			}

			stringBuilder.Clear();
			stringBuilder.Append(dirEnterIndicator);

			using(MemoryStream ms = new MemoryStream(capture))
			using (StreamReader sr = new StreamReader(ms))
			{
				sr.ReadLine();
				sr.ReadLine();
				sr.ReadLine();
				string line;
				for (; !sr.EndOfStream;)
				{
					//Console.WriteLine(stringBuilder);
					line = sr.ReadLine();
					if (line.EndsWith(dirEnterIndicator))
					{
						stringBuilder.Append(line);
						line.TrimEnd(dirEnterIndicator);
						if (line.Contains(stringToSearch))
						{
							dirs.Add(stringBuilder.ToString());
						}
					}
					else if (line.EndsWith(dirLeaveIndicator))
					{
						StrBldrRemoveLastDir();
					}
					else
					{
						if (line.Contains(stringToSearch))
						{
							files.Add(stringBuilder.ToString() + line);
						}
					}
				}
			}

			return (files, dirs);
		}
	}
}
