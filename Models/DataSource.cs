using System;
using System.Collections.Generic;
using System.Text;
using ColdStorageManager.DBManagers;
using static ColdStorageManager.Globals;

namespace ColdStorageManager.Models
{
	// controller class for handling DbManagers and caching
	public class DataSource
	{
		public string Name { get; set; }

		public IDbManager DbManager { get; set; }

		public bool IsConnected => DbManager.IsConnected;

		private List<Capture> _capturesCache;
		private DateTime _capturesCacheDT;
		private bool _cacheInvalidated = false;

		public DataSource(DbConnectionProfile profile)
		{
			Name = profile.ProfileName;
			if (Name.Equals(GetLocalizedString(ConfigNameOfLocalDS)))
			{
				DbManager = new SQLiteDbManager();
			}
			else
			{
				DbManager = new MySQLDbManager(profile);
			}
		}

		private bool IsCacheValid()
		{
			bool ret = _capturesCache != null && 
				(((DbManager is SQLiteDbManager) || ((DateTime.Now - _capturesCacheDT).TotalSeconds < remoteCaptureCacheTTL)) && !_cacheInvalidated);

			//assuming cache will be refreshed after it is found manually invalidated
			_cacheInvalidated = false;

			return ret;
		}

		public List<Capture> GetCaptures()
		{
			//if the cache is expired, refresh it
			if (!IsCacheValid())
			{
				_capturesCache = DbManager.GetCaptures();
				_capturesCacheDT = DateTime.Now;
			}
			return _capturesCache;
		}

		public void SaveCapture(Capture capture)
		{
			DbManager.SaveCapture(capture);
			InvalidateCache();
		}

		// id is contained in Capture instance itself
		public void UpdateCaptureById(Capture capture)
		{
			DbManager.UpdateCaptureById(capture);
			InvalidateCache();
		}

		public void DeleteCapture(Capture capture)
		{
			DbManager.DeleteCapture(capture);
			InvalidateCache();
		}

		public bool DeleteAllCaptures()
		{
			InvalidateCache();

			return DbManager.DeleteAllCaptures();
		}

		public void CloseConnection()
		{
			DbManager.CloseConnection();
			InvalidateCache();
		}

		public bool ConnectAndCreateTable()
		{
			return DbManager.ConnectAndCreateTable();
		}

		public bool CreateTable(string tableName, List<ColumnInfo> columns)
		{
			return DbManager.CreateTable(tableName, columns);
		}

		public void InvalidateCache()
		{
			_capturesCache?.Clear();
			_cacheInvalidated = true;
		}

		public override string ToString()
		{
			return Name;
		} 
	}
}
