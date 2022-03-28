using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using ColdStorageManager.DBManagers;
using ColdStorageManager.Models;
using Dapper;

namespace ColdStorageManager
{
	public class SQLiteDbManager : IDbManager
	{
		const string dbPath = "Database/";
		const string dbCapturesFile = "Captures.sqlite";
		const string dbCapturesTableName = "Captures";

		private static string[] dbCapturesColumns = new string[]
		{
			"id", "drive_model", "drive_sn", "isNVMe", "NVMeSerialNumberDetectionFail",
			"drive_size", "drive_nickname", "partition_label", "partition_number", "partition_size",
			"partition_free_space", "volume_guid",
			"capture_properties", "capture_datetime", "capture_lines_number", "capture_files_number",
			"capture_directories_number",
			"capture", "sizes", "creation_times", "last_access_times", "last_mod_times"
		};

		private static string[] dbCapturesColumnsWithoutId = new string[]
		{
			"drive_model", "drive_sn", "isNVMe", "NVMeSerialNumberDetectionFail",
			"drive_size", "drive_nickname", "partition_label", "partition_number", "partition_size",
			"partition_free_space", "volume_guid",
			"capture_properties", "capture_datetime", "capture_lines_number", "capture_files_number",
			"capture_directories_number",
			"capture", "sizes", "creation_times", "last_access_times", "last_mod_times"
		};

		private static string[] dbCapturesColumnsProperties = new string[]
		{
			"INTEGER PRIMARY KEY AUTOINCREMENT", "TEXT", "TEXT", "INTEGER", "INTEGER",
			"INTEGER", "TEXT", "TEXT", "INTEGER", "INTEGER",
			"INTEGER", "TEXT",
			"INTEGER", "TEXT", "INTEGER", "INTEGER",
			"INTEGER",
			"BLOB", "BLOB", "BLOB", "BLOB", "BLOB"
		};

		private static string CreateCapturesTableSQLCommand;
		private static string AddCaptureSQLCommand;
		private static string UpdateCaptureSQLCommand;

		private StringBuilder stringBuilder;
		SQLiteConnection dbConnection;
		SQLiteCommand command;
		string sqlCommand;
		string dbCapturesFilePath;
		public bool IsConnectionSuccessful = false;

		public SQLiteDbManager()
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

			//constructing sql commands
			stringBuilder = new StringBuilder();
			ConstructCreateSQLCommand();
			ConstructAddCaptureSQLCommand();
			ConstructUpdateCaptureSQLCommand();

			dbConnection = new SQLiteConnection("Data Source=" + dbCapturesFilePath+ ";Version=3;");
			dbConnection.Open();
			Globals.dbStatusEllipse.Fill = new SolidColorBrush(Color.FromRgb(0,255,0));
			Globals.dbStatusBarTb.Text = Application.Current.Resources["db_connect_suc"] + dbCapturesFilePath;
			IsConnectionSuccessful = true;
			command = dbConnection.CreateCommand();
			CreateTableIfNotFound(dbCapturesTableName);
		}

		~SQLiteDbManager()
		{
			dbConnection.Close();
		}

		private void ConstructCreateSQLCommand()
		{
			stringBuilder.Clear();
			stringBuilder.Append("CREATE TABLE '").Append(dbCapturesTableName).Append("'(");
			int i;
			for (i = 0; i < dbCapturesColumns.Length-1; i++)
			{
				stringBuilder.Append(dbCapturesColumns[i]).Append(" ").Append(dbCapturesColumnsProperties[i])
					.Append(", ");
			}
			stringBuilder.Append(dbCapturesColumns[i]).Append(" ").Append(dbCapturesColumnsProperties[i]).Append(")");
			CreateCapturesTableSQLCommand = stringBuilder.ToString();
		}

		private void ConstructAddCaptureSQLCommand()
		{
			AddCaptureSQLCommand = "insert into " + dbCapturesTableName + " (" + String.Join(", ", dbCapturesColumnsWithoutId) +
			                       ") values (@" + String.Join(", @", dbCapturesColumnsWithoutId) + ")";
		}

		private void ConstructUpdateCaptureSQLCommand()
		{
			stringBuilder.Clear();
			stringBuilder.Append("update ").Append(dbCapturesTableName).Append(" set ");
			int i;
			for (i = 0; i < dbCapturesColumnsWithoutId.Length-1; i++)
			{
				stringBuilder.Append(dbCapturesColumnsWithoutId[i]).Append(" = @")
					.Append(dbCapturesColumnsWithoutId[i]).Append(", ");
			}

			stringBuilder.Append(dbCapturesColumnsWithoutId[i]).Append(" = @")
				.Append(dbCapturesColumnsWithoutId[i]);

			stringBuilder.Append(" where ").Append(dbCapturesColumns[0]).Append(" = @").Append(dbCapturesColumns[0]);
			UpdateCaptureSQLCommand = stringBuilder.ToString();
		}

		public List<Capture> GetCaptures()
		{
			var output = dbConnection.Query<Capture>("select * from " + dbCapturesTableName, new DynamicParameters());
			return output.ToList();
		}

		public void SaveCapture(Capture capture)
		{
			dbConnection.Execute(AddCaptureSQLCommand, capture);
		}

		public void UpdateCaptureById(Capture capture)
		{
			dbConnection.Execute(UpdateCaptureSQLCommand, capture);
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
				command.CommandText = CreateCapturesTableSQLCommand;
				try
				{
					int res = command.ExecuteNonQuery();
					Globals.dbStatusBarTb.Text = Application.Current.Resources["db_create_suc"] + dbCapturesTableName;
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
					Console.WriteLine(e.Data.ToString());
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
