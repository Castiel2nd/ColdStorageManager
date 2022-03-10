using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using ColdStorageManager.Models;
using Dapper;

namespace ColdStorageManager
{
	public class DbManager
	{
		SQLiteConnection dbConnection;
		SQLiteCommand command;
		string sqlCommand;
		string dbPath = "Database/";
		string dbCapturesFile = "Captures.sqlite";
		string dbCapturesFilePath;
		string dbCapturesTableName = "Captures";
		public bool IsConnectionSuccessful = false;

		public DbManager()
		{
			dbCapturesFilePath = dbPath + dbCapturesFile;
			if (!Directory.Exists(dbPath))
			{
				Directory.CreateDirectory(dbPath);
				Globals.dbStatusBarTb.Text = Application.Current.Resources["db_dir_create_suc"] + dbPath;
			}

			if (!File.Exists(dbCapturesFilePath))
			{
				SQLiteConnection.CreateFile(dbCapturesFilePath);
				Globals.dbStatusBarTb.Text = Application.Current.Resources["db_file_create_suc"] + dbCapturesFilePath;
			}
			dbConnection = new SQLiteConnection("Data Source=" + dbCapturesFilePath+ ";Version=3;");
			dbConnection.Open();
			Globals.dbStatusEllipse.Fill = new SolidColorBrush(Color.FromRgb(0,255,0));
			Globals.dbStatusBarTb.Text = Application.Current.Resources["db_connect_suc"] + dbCapturesFilePath;
			IsConnectionSuccessful = true;
			command = dbConnection.CreateCommand();
			CreateTableIfNotFound(dbCapturesTableName);
		}

		~DbManager()
		{
			dbConnection.Close();
		}

		public List<Capture> GetCaptures()
		{
			var output = dbConnection.Query<Capture>("select * from " + dbCapturesTableName, new DynamicParameters());
			return output.ToList();
		}

		public void SaveCapture(Capture capture)
		{
			dbConnection.Execute(
				"insert into " + dbCapturesTableName +
				" (drive_model, drive_sn, isNVMe, NVMeSerialNumberDetectionFail, drive_size, drive_nickname, partition_label, partition_number, partition_size, partition_free_space, volume_guid, capture_properties, capture_datetime, capture_lines_number, " +
				" capture_files_number, capture_directories_number, capture, sizes, creation_times, last_access_times, last_mod_times) " +
				"values (@drive_model, @drive_sn, @isNVMe, @NVMeSerialNumberDetectionFail, @drive_size, @drive_nickname, @partition_label, @partition_number, @partition_size, @partition_free_space, @volume_guid, @capture_properties, @capture_datetime, @capture_lines_number," +
				" @capture_files_number, @capture_directories_number, @capture, @sizes, @creation_times, @last_access_times, @last_mod_times)",
				capture);
		}

		public void DeleteCapture(Capture capture)
		{
			dbConnection.Execute("delete from " + dbCapturesTableName +
			                     " where drive_model = @drive_model and drive_sn = @drive_sn", capture);
		}

		private void CreateTableIfNotFound(string tableName)
		{
			if (!checkIfTableExists(tableName))
			{
				command.CommandText =
					"CREATE TABLE 'Captures'(id INTEGER PRIMARY KEY AUTOINCREMENT, drive_model TEXT, drive_sn TEXT, isNVMe INTEGER, NVMeSerialNumberDetectionFail INTEGER, drive_size INTEGER," +
					" drive_nickname TEXT, partition_label TEXT, partition_number INTEGER, partition_size INTEGER, partition_free_space INTEGER, volume_guid TEXT," +
					"capture_properties INTEGER, capture_datetime TEXT, capture_lines_number INTEGER, capture_files_number INTEGER, capture_directories_number INTEGER, " +
					"capture BLOB, sizes BLOB, creation_times BLOB, last_access_times BLOB, last_mod_times BLOB)";
				try
				{
					int res = command.ExecuteNonQuery();
					Globals.dbStatusBarTb.Text = Application.Current.Resources["db_create_suc"] + dbCapturesTableName;
				}
				catch (Exception)
				{

				}
			}
			else
			{
				Globals.dbStatusBarTb.Text = Application.Current.Resources["db_table_found"] + dbCapturesTableName;
			}
		}

		private bool checkIfTableExists(string tableName)
		{
			command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='" + dbCapturesTableName +
			                      "'";
			var res = command.ExecuteScalar();

			return res != null && res.ToString() == dbCapturesTableName;
		}
	}
}
