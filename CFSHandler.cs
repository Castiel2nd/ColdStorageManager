using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ColdStorageManager
{
	internal static class CFSHandler
	{
		private static string fileExtension = ".cfs";
		private static string fileTypeIdentifier = "Cold_Storage_Manager-Captured_File_System";
		private static string version = Globals.version;
		private static string dirEnterIndicator = @"\";
		private static string dirLeaveIndicator = @"/";

		//writes CFS to file
		public static bool WriteCFS(string fileName, string driveModel, string nickname)
		{
			using (MemoryStream ms = new MemoryStream(1000))
			{
				StreamWriter sw = new StreamWriter(ms);
				WriteToStream(sw, driveModel, nickname);

				using (FileStream fs = new FileStream(fileName + fileExtension, FileMode.Create, FileAccess.Write))
				{
					ms.WriteTo(fs);
				}

				sw.Close();
			}

			using (FileStream fs = new FileStream(fileName + fileExtension, FileMode.Open, FileAccess.Read))
			using (StreamReader sr = new StreamReader(fs))
			{
				string content = sr.ReadToEnd();
			}

			return true;
		}

		public static byte[] GetCFSBytes(string driveModel, string nickname)
		{
			byte[] ret;
			using (MemoryStream ms = new MemoryStream(1000))
			{
				StreamWriter sw = new StreamWriter(ms);
				WriteToStream(sw, driveModel, nickname);
				ret = ms.ToArray();
			}

			return ret;
		}

		private static void WriteToStream(StreamWriter sw, string driveModel, string nickname)
		{
			List<CSMFileSystemEntry> list = Globals.fsList;
			sw.WriteLine(fileTypeIdentifier + "#" + version);
			sw.WriteLine(driveModel);
			sw.WriteLine(nickname);
			List<List<CSMFileSystemEntry>> listRefs =
				new List<List<CSMFileSystemEntry>>();
			List<int> indexes = new List<int>();
			int j = 0;
			listRefs.Add(list); indexes.Add(0);
			bool breaking = false;
			for (int i = 0; ;)
			{
				while (i == listRefs[j].Count)
				{
					sw.WriteLine(dirLeaveIndicator);
					if (j == 0)
					{
						breaking = true;
						break;
					}
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
					sw.WriteLine(entry.Name + dirEnterIndicator);
					if (!dir.IsEmpty)
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
					sw.WriteLine(entry.Name);
				}
			}
			sw.Flush();
		}

	}
}
