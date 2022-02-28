using System;
using System.Collections.Generic;
using System.Text;

namespace ColdStorageManager.Models
{
	public abstract class SearchResult
	{

	}

	public class SearchResultCaptureView : SearchResult
	{

		public List<SearchResultView> SearchResults { get; set; }
		public CapturePhDisk PhDisk { get; set; }
		public Capture Capture { get; set; }

		public SearchResultCaptureView(CapturePhDisk phDisk, Capture capture)
		{
			PhDisk = phDisk;
			Capture = capture;
			SearchResults = new List<SearchResultView>();
		}

		public SearchResultCaptureView(CapturePhDisk phDisk, Capture capture, List<SearchResultView> searchResults)
		{
			PhDisk = phDisk;
			Capture = capture;
			SearchResults = searchResults;
		}
	}

	public class SearchResultView : SearchResult
	{
		private bool sizeEnabled;
		private bool createTimeEnabled;
		private bool accessTimeEnabled;
		private bool modTimeEnabled;

		private long size;
		private DateTime createDT;
		private DateTime lastAccessDT;
		private DateTime lastModDT;

		public string Name { get; set; }
		public string Path { get; set; }

		public long Size
		{
			get
			{
				return size;
			}
			set
			{
				size = value;
			}
		}

		public DateTime CreateDT
		{
			get
			{
				return createDT;
			}
			set
			{
				createDT = value;
			}
		}

		public DateTime LastAccessDT
		{
			get
			{
				return lastAccessDT;
			}
			set
			{
				lastAccessDT = value;
			}
		}

		public DateTime LastModDT
		{
			get
			{
				return lastModDT;
			}
			set
			{
				lastModDT = value;
			}
		}

		public string SizeStr
		{
			get
			{
				return sizeEnabled ? size.ToString() : string.Empty;
			}
		}

		public string CreateDTStr
		{
			get
			{
				return createTimeEnabled ? createDT.ToString() : string.Empty;
			}
		}

		public string LastAccessDTStr
		{
			get
			{
				return accessTimeEnabled ? lastAccessDT.ToString() : string.Empty;
			}
		}

		public string LastModDTStr
		{
			get
			{
				return modTimeEnabled ? lastModDT.ToString() : string.Empty;
			}
		}

		public SearchResultView(string name, string path, long size = -1, DateTime createDt = default, DateTime lastAccessDt = default, DateTime lastModDt = default)
		{
			Name = name;
			Path = path;
			Size = size;
			CreateDT = createDt;
			LastAccessDT = lastAccessDt;
			LastModDT = lastModDt;
			sizeEnabled = size >= 0;
			createTimeEnabled = createDt != default;
			accessTimeEnabled = lastAccessDt != default;
			modTimeEnabled = lastModDt != default;
		}
	}
}
