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

		public const int LESS_THAN = 0;
		public const int LESS_THAN_EQ = 1;
		public const int EQUAL = 2;
		public const int MORE_THAN_EQ = 3;
		public const int MORE_THAN = 4;

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

		public static (byte[] capture, byte[] sizes, byte[] creation_times, byte[] last_access_times, byte[] last_mod_times, uint lines,
			uint files, uint dirs)
			GetCFSBytes(string driveModel, string nickname, bool size = false, bool createTime  = false,
			bool accessTime = false, bool modTime = false)
		{
			(byte[] capture, byte[] sizes, byte[] creation_times, byte[] last_access_times, byte[] last_mod_times, uint lines,
					uint files, uint dirs) ret;

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

			var entryNrs = WriteToStreams(swArray, driveModel, nickname, size, createTime, accessTime, modTime);
			ret.lines = entryNrs.lines;
			ret.files = entryNrs.files;
			ret.dirs = entryNrs.dirs;
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
				MemoryStream[] msArray = new MemoryStream[numBlobTypes-1];
				StreamReader[] srArray = new StreamReader[numBlobTypes - 1];
				var bools = Globals.DecodeCaptureProperties(capture.capture_properties);
				bool size = bools.size;
				bool createTime = bools.createTime;
				bool lastAccessTime = bools.lastAccessTime;
				bool lastModTime = bools.lastModTime;
				uint allEntries = capture.capture_files_number + capture.capture_directories_number;
				long[] sizes_prepared = null;
				DateTime[] creation_times_prepared = null, last_access_times_prepared = null, last_mod_times_prepared = null;
				if (size)
				{
					capture.sizes_prepared = new long[allEntries];
					sizes_prepared = capture.sizes_prepared;
					msArray[0] = new MemoryStream(capture.sizes);
					srArray[0] = new StreamReader(msArray[0]);
				}
				if (createTime)
				{
					capture.creation_times_prepared = new DateTime[allEntries];
					creation_times_prepared = capture.creation_times_prepared;
					msArray[1] = new MemoryStream(capture.creation_times);
					srArray[1] = new StreamReader(msArray[1]);
				}
				if (lastAccessTime)
				{
					capture.last_access_times_prepared = new DateTime[allEntries];
					last_access_times_prepared = capture.last_access_times_prepared;
					msArray[2] = new MemoryStream(capture.last_access_times);
					srArray[2] = new StreamReader(msArray[2]);
				}
				if (lastModTime)
				{
					capture.last_mod_times_prepared = new DateTime[allEntries];
					last_mod_times_prepared = capture.last_mod_times_prepared;
					msArray[3] = new MemoryStream(capture.last_mod_times);
					srArray[3] = new StreamReader(msArray[3]);
				}
				uint entries = 0;
				string line, sideString;
				using MemoryStream ms = new MemoryStream(capture.capture);
				using (StreamReader sr = new StreamReader(ms))
				{
					//read headers on all streams
					foreach (var streamReader in srArray)
					{
						streamReader?.ReadLine();
						streamReader?.ReadLine();
						streamReader?.ReadLine();
					}
					sr.ReadLine();
					sr.ReadLine();
					sr.ReadLine();
					
					while (!sr.EndOfStream)
					{
						line = sr.ReadLine();

						//it's a directory
						if (line.EndsWith(dirEnterIndicator))
						{
							if (size)
								srArray[0].ReadLine();
							if(createTime)
								creation_times_prepared[entries] = DateTime.Parse(srArray[1].ReadLine());
							if(lastAccessTime)
								last_access_times_prepared[entries] = DateTime.Parse(srArray[2].ReadLine());
							if (lastModTime)
								last_mod_times_prepared[entries] = DateTime.Parse(srArray[3].ReadLine());
							entries++;
						}
						else if (!line.EndsWith(dirLeaveIndicator))
						{
							if (size){}
								sizes_prepared[entries] = long.Parse(srArray[0].ReadLine());
							if (createTime)
								creation_times_prepared[entries] = DateTime.Parse(srArray[1].ReadLine());
							if (lastAccessTime)
								last_access_times_prepared[entries] = DateTime.Parse(srArray[2].ReadLine());
							if (lastModTime)
								last_mod_times_prepared[entries] = DateTime.Parse(srArray[3].ReadLine());
							entries++;
						}
						else
						{
							if (size) { }
								srArray[0].ReadLine();
							if (createTime)
								srArray[1].ReadLine(); 
							if (lastAccessTime)
								srArray[2].ReadLine(); 
							if (lastModTime)
								srArray[3].ReadLine();
						}
					}
				}
			}
		}

		private static (uint lines, uint files, uint dirs) WriteToStreams(StreamWriter[] swArray, string driveModel, string nickname, bool size = false, bool createTime = false,
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
			uint lines = 3, files = 0, dirs = 0;
			string entryStr;
			for (int i = 0; ;)
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
				if (entry is CSMDirectory { IsChecked: true } dir)
				{
					lines++;
					dirs++;
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
				else if(entry is CSMFile { IsChecked: true } file)
				{
					lines++;
					files++;
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

			return (lines, files, dirs);
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

		public static (List<SearchResultView> files, List<SearchResultView> dirs) Search(byte[] capture, ushort captureProperties, ushort searchProperties,
			string stringToSearch,
			 int sizeRelation, long sizeToSearch,
			 int createTimeRelation, DateTime createTimeToSearch,
			 int accessTimeRelation, DateTime accessTimeToSearch,
			 int lastModTimeRelation, DateTime lastModTimeToSearch,
			 in long[] sizesPrepared, in DateTime[] createTimesPrepared, in DateTime[] accessTimesPrepared, in DateTime[] modTimesPrepared)
		{
			List<SearchResultView> files = new List<SearchResultView>();
			List<SearchResultView> dirs = new List<SearchResultView>();

			if (stringBuilder == null)
			{
				stringBuilder = new StringBuilder();
			}

			var capturePropDecoded = Globals.DecodeCaptureProperties(captureProperties);
			bool sizeInCapture = capturePropDecoded.size,
				createTimeInCapture = capturePropDecoded.createTime,
				accessTimeInCapture = capturePropDecoded.lastAccessTime,
				modTimeInCapture = capturePropDecoded.lastModTime;
			var searchPropDecoded = Globals.DecodeCaptureProperties(searchProperties);
			bool searchInSize = searchPropDecoded.size,
				searchInCreateTime = searchPropDecoded.createTime,
				searchInAccessTime = searchPropDecoded.lastAccessTime,
				searchInModTime = searchPropDecoded.lastModTime;

			stringBuilder.Clear();
			stringBuilder.Append(dirEnterIndicator);

			using(MemoryStream ms = new MemoryStream(capture))
			using (StreamReader sr = new StreamReader(ms))
			{
				sr.ReadLine();
				sr.ReadLine();
				sr.ReadLine();
				uint arrayHelperIndex = 0;
				string line;
				for (; !sr.EndOfStream;)
				{
					//Console.WriteLine(stringBuilder);
					line = sr.ReadLine();
					if (line.EndsWith(dirEnterIndicator))
					{
						stringBuilder.Append(line);
						line.TrimEnd(dirEnterIndicator);
						if (line.Contains(stringToSearch)
						    &&
						    (!searchInCreateTime || (!createTimeInCapture || DateRelationCondition(createTimesPrepared[arrayHelperIndex].Date, createTimeToSearch, createTimeRelation)))
						    &&
						    (!searchInAccessTime || (!accessTimeInCapture || DateRelationCondition(accessTimesPrepared[arrayHelperIndex].Date, accessTimeToSearch, accessTimeRelation)))
						    &&
						    (!searchInModTime || (!modTimeInCapture || DateRelationCondition(modTimesPrepared[arrayHelperIndex].Date, lastModTimeToSearch, lastModTimeRelation))))
						{
							dirs.Add(new SearchResultView(line, stringBuilder.ToString(), 
																-1, createTimeInCapture ? createTimesPrepared[arrayHelperIndex] : default(DateTime), 
																accessTimeInCapture ? accessTimesPrepared[arrayHelperIndex] : default(DateTime),
																modTimeInCapture ? modTimesPrepared[arrayHelperIndex] : default(DateTime)));
						}
						arrayHelperIndex++;
					}
					else if (line.EndsWith(dirLeaveIndicator))
					{
						StrBldrRemoveLastDir();
					}
					else
					{
						if (line.Contains(stringToSearch)
						    &&
						    (!searchInSize || ( !sizeInCapture || SizeRelationCondition(sizesPrepared[arrayHelperIndex], sizeToSearch, sizeRelation)))
							&&
						    (!searchInCreateTime || (!createTimeInCapture || DateRelationCondition(createTimesPrepared[arrayHelperIndex].Date, createTimeToSearch, createTimeRelation)))
						    &&
						    (!searchInAccessTime || (!accessTimeInCapture || DateRelationCondition(accessTimesPrepared[arrayHelperIndex].Date, accessTimeToSearch, accessTimeRelation)))
						    &&
						    (!searchInModTime || (!modTimeInCapture || DateRelationCondition(modTimesPrepared[arrayHelperIndex].Date, lastModTimeToSearch, lastModTimeRelation))))
						{
							files.Add(new SearchResultView(line, stringBuilder.ToString() + line,
								sizeInCapture ? sizesPrepared[arrayHelperIndex] : -1,
								createTimeInCapture ? createTimesPrepared[arrayHelperIndex] : default(DateTime),
								accessTimeInCapture ? accessTimesPrepared[arrayHelperIndex] : default(DateTime),
								modTimeInCapture ? modTimesPrepared[arrayHelperIndex] : default(DateTime)));
						}

						arrayHelperIndex++;
					}
				}
			}

			return (files, dirs);
		}

		private static bool SizeRelationCondition(long size, long sizeToSearch, int relation)
		{
			switch (relation)
			{
				case LESS_THAN:
					return size < sizeToSearch;
				case LESS_THAN_EQ:
					return size <= sizeToSearch;
				case EQUAL:
					return size == sizeToSearch;
				case MORE_THAN_EQ:
					return size >= sizeToSearch;
				case MORE_THAN:
					return size > sizeToSearch;
				default:
					return false;
			}
		}

		private static bool DateRelationCondition(DateTime size, DateTime sizeToSearch, int relation)
		{
			switch (relation)
			{
				case LESS_THAN:
					return size < sizeToSearch;
				case LESS_THAN_EQ:
					return size <= sizeToSearch;
				case EQUAL:
					return size == sizeToSearch;
				case MORE_THAN_EQ:
					return size >= sizeToSearch;
				case MORE_THAN:
					return size > sizeToSearch;
				default:
					return false;
			}
		}
	}
}
