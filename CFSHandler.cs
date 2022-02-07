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
		private static char dirEnterIndicator = '\\';
		private static char dirLeaveIndicator = '/';
		private static StringBuilder stringBuilder;

		public static bool WriteCFSToFile(string fileName, string driveModel, string nickname)
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
					if (j == 0)
					{
						breaking = true;
						break;
					}
					sw.WriteLine(dirLeaveIndicator);
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
					if (dir.IsEmpty)
					{
						sw.WriteLine(dirLeaveIndicator);
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
					sw.WriteLine(entry.Name);
				}
			}
			sw.Flush();
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
