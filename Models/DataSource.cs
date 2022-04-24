using System;
using System.Collections.Generic;
using System.Text;
using ColdStorageManager.DBManagers;
using static ColdStorageManager.Globals;
using static ColdStorageManager.Logger;

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

		public List<string> GetTableNames()
		{
			return DbManager.GetTableNames();
		}

		public ColumnInfo[] GetColumns(string tableName)
		{
			return DbManager.GetColumns(tableName);
		}

		public bool SetTableData(ref TableModel table)
		{
			return DbManager.SetTableData(ref table);
		}

		public bool SetCellData(string tableName, string columnName, object rowId, object data)
		{
			return DbManager.SetCellData(tableName, columnName, rowId, data);
		}

		public object GetLastInsertedRowId()
		{
			return DbManager.GetLastInsertedRowId();
		}

		public object[] GetLastInsertedRow(string tableName, ColumnInfo[] columns)
		{
			return DbManager.GetLastInsertedRow(tableName, columns);
		}

		public object[] GetRowById(string tableName, ColumnInfo[] columns, object rowId)
		{
			return DbManager.GetRowById(tableName, columns, rowId);
		}

		public bool DeleteRowById(string tableName, object rowId)
		{
			return DbManager.DeleteRowById(tableName, rowId);
		}

		public bool DeleteTable(string tableName)
		{
			return DbManager.DeleteTable(tableName);
		}

		public bool TryParse(CSMColumnType columnType, object data)
		{
			if (data == null)
			{
				if (debugLogging)
				{
					LogDbAction($"Tried parsing null data. ColumnType = {columnType}", WARNING);
				}

				return false;
			}

			return DbManager.TryParse(columnType, data);
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
