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
using static ColdStorageManager.Globals;
using static ColdStorageManager.Logger;

namespace ColdStorageManager
{
	public class SQLiteDbManager : IDbManager
	{
		private const string dbDirName = "Database";
		private const string dbPath = dbDirName + "/";
		private const string dbCapturesFile = "Captures.sqlite";
		private const string dbCapturesFilePath = dbPath + dbCapturesFile;

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
		
		public bool IsConnectionSuccessful = false;

		public bool IsConnected
		{
			get
			{
				if (dbConnection == null)
				{
					return false;
				}
				return dbConnection.State != ConnectionState.Closed && dbConnection.State != ConnectionState.Broken;
			}
		}

		public SQLiteDbManager()
		{
			if (!Directory.Exists(dbPath))
			{
				Directory.CreateDirectory(dbPath);
				LogDbAction(
					$"{GetLocalizedString("db_dir_create_suc")}{dbPath}",
					$"Successfully created db. directory:{dbPath}.");
			}

			if (!File.Exists(dbCapturesFilePath))
			{
				SQLiteConnection.CreateFile(dbCapturesFilePath);
				LogDbAction(
					$"{GetLocalizedString("db_file_create_suc")}{dbCapturesFilePath}",
					$"Successfully created db. file:{dbCapturesFilePath}.");
			}

			//constructing sql commands
			stringBuilder = new StringBuilder();
			ConstructCreateSQLCommand();
			ConstructAddCaptureSQLCommand();
			ConstructUpdateCaptureSQLCommand();

			dbConnection = new SQLiteConnection("Data Source=" + dbCapturesFilePath+ ";Version=3;");
			dbConnection.Open();
			Logger.dbStatusEllipse.Fill = new SolidColorBrush(Color.FromRgb(0,255,0));
			LogDbAction($"{GetLocalizedString("db_connect_suc")}{dbCapturesFilePath}",
				$"Successfully connected to db.:{dbCapturesFilePath}.");
			IsConnectionSuccessful = true;
			command = dbConnection.CreateCommand();
			CreateTableIfNotFound(IDbManager.DbCapturesTableName);
		}

		~SQLiteDbManager()
		{
			if (dbConnection.State != ConnectionState.Closed)
			{
				CloseConnection();
			}
		}

		private void ConstructCreateSQLCommand()
		{
			stringBuilder.Clear();
			stringBuilder.Append("CREATE TABLE '").Append(IDbManager.DbCapturesTableName).Append("'(");
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
			AddCaptureSQLCommand = "insert into " + IDbManager.DbCapturesTableName + " (" + String.Join(", ", dbCapturesColumnsWithoutId) +
			                       ") values (@" + String.Join(", @", dbCapturesColumnsWithoutId) + ")";
		}

		private void ConstructUpdateCaptureSQLCommand()
		{
			stringBuilder.Clear();
			stringBuilder.Append("update ").Append(IDbManager.DbCapturesTableName).Append(" set ");
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
			IEnumerable<Capture> ret = null;

			try
			{
				ret = dbConnection.Query<Capture>("select * from " + IDbManager.DbCapturesTableName, new DynamicParameters());

				LogDbAction($"{GetLocalizedString("db_list_suc")} {IDbManager.DbCapturesTableName}",
					$"Successfully loaded captures from: {dbCapturesFilePath}.");

			}
			catch (Exception e)
			{
				LogDbAction($"{GetLocalizedString("db_list_fail")} {IDbManager.DbCapturesTableName}",
					$"Failed to load captures from: {dbCapturesFilePath}.{LineSeparator}" +
					$"{e.Message}",
					ERROR);

			}

			return ret.ToList();
		}

		public void SaveCapture(Capture capture)
		{
			try
			{
				dbConnection.Execute(AddCaptureSQLCommand, capture);

				LogDbAction($"{GetLocalizedString("db_save_suc")} {IDbManager.DbCapturesTableName}",
					$"Successfully saved a capture to: {dbCapturesFilePath}.{LineSeparator}" +
					$"({nameof(capture.drive_model)} = {capture.drive_model}, {nameof(capture.drive_sn)} = {capture.drive_sn})");

			}
			catch (Exception e)
			{
				LogDbAction($"{GetLocalizedString("db_save_fail")} {IDbManager.DbCapturesTableName}",
					$"Failed to save a capture to: {dbCapturesFilePath}.{LineSeparator}" +
					$"({nameof(capture.drive_model)} = {capture.drive_model}, {nameof(capture.drive_sn)} = {capture.drive_sn}){LineSeparator}" +
					$"{e.Message}",
					ERROR);

			}
		}

		public void UpdateCaptureById(Capture capture)
		{
			try
			{
				dbConnection.Execute(UpdateCaptureSQLCommand, capture);

				LogDbAction($"{GetLocalizedString("db_update_suc")} {IDbManager.DbCapturesTableName}",
					$"Successfully updated a capture in: {dbCapturesFilePath}. (id = {capture.id})");

			}
			catch (Exception e)
			{
				LogDbAction($"{GetLocalizedString("db_update_fail")} {IDbManager.DbCapturesTableName}",
					$"Failed to update a capture in: {dbCapturesFilePath}. (id = {capture.id}){LineSeparator}" +
					$"{e.Message}",
					ERROR);

			}
		}

		public void DeleteCapture(Capture capture)
		{
			try
			{
				dbConnection.Execute("delete from " + IDbManager.DbCapturesTableName +
									 " where drive_model = @drive_model and drive_sn = @drive_sn and volume_guid = @volume_guid", capture);

				LogDbAction($"{GetLocalizedString("db_delete_suc")} {IDbManager.DbCapturesTableName}",
					$"Successfully deleted a capture from: {dbCapturesFilePath}. (id = {capture.id})");

			}
			catch (Exception e)
			{
				LogDbAction($"{GetLocalizedString("db_delete_fail")} {IDbManager.DbCapturesTableName}",
					$"Failed to delete a capture from: {dbCapturesFilePath}. (id = {capture.id}){LineSeparator}" +
					$"{e.Message}",
					ERROR);

			}
		}

		public void CloseConnection()
		{
			dbConnection.Close();

			LogDbAction($"{GetLocalizedString("db_connect_closed")} {IDbManager.DbCapturesTableName}",
				$"Closed connection to: {dbCapturesFilePath}.");
		}

		public bool ConnectAndCreateTableIfNotFound()
		{
			return IsConnected;
		}

		private void CreateTableIfNotFound(string tableName)
		{
			if (!checkIfTableExists(tableName))
			{
				command.CommandText = CreateCapturesTableSQLCommand;
				try
				{
					int res = command.ExecuteNonQuery();
					LogDbAction(
						$"{GetLocalizedString("db_table_create_suc")}{dbCapturesFilePath}\\{IDbManager.DbCapturesTableName}",
						$"Successfully created table:{dbCapturesFilePath}\\{IDbManager.DbCapturesTableName}.");
				}
				catch (Exception e)
				{
					LogDbActionWithMsgBox($"{GetLocalizedString("db_table_create_fail")} ",
						$"Failed to create table: {dbCapturesFilePath}/{IDbManager.DbCapturesTableName}.",
						GetLocalizedString("db_table_create_fail_title"), MessageBoxImage.Error, e.Message);

				}
			}
			else
			{
				LogDbAction(
					$"{GetLocalizedString("db_table_found")}{IDbManager.DbCapturesTableName}{GetLocalizedString("db_table_found2")}{dbCapturesFilePath}",
					$"Found table '{IDbManager.DbCapturesTableName}' in database:{dbCapturesFilePath}.");
			}
		}

		private bool checkIfTableExists(string tableName)
		{
			command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='" + IDbManager.DbCapturesTableName +
			                      "'";
			var res = command.ExecuteScalar();

			return res != null && res.ToString() == IDbManager.DbCapturesTableName;
		}
	}
}
