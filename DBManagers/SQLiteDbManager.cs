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
		private const string dbRegisterFile = "Register.sqlite";
		private const string dbCapturesFilePath = dbPath + dbCapturesFile;
		private const string dbRegisterFilePath = dbPath + dbRegisterFile;

		private static readonly string[] ColTypes = { "TEXT", "INTEGER" };

		private static readonly string[] DbCapturesColumns = new string[]
		{
			"id", "drive_model", "drive_sn", "isNVMe", "NVMeSerialNumberDetectionFail",
			"drive_size", "drive_nickname", "partition_label", "partition_number", "partition_size",
			"partition_free_space", "volume_guid",
			"capture_properties", "capture_datetime", "capture_lines_number", "capture_files_number",
			"capture_directories_number",
			"capture", "sizes", "creation_times", "last_access_times", "last_mod_times"
		};

		private static readonly string[] DbCapturesColumnsWithoutId = new string[]
		{
			"drive_model", "drive_sn", "isNVMe", "NVMeSerialNumberDetectionFail",
			"drive_size", "drive_nickname", "partition_label", "partition_number", "partition_size",
			"partition_free_space", "volume_guid",
			"capture_properties", "capture_datetime", "capture_lines_number", "capture_files_number",
			"capture_directories_number",
			"capture", "sizes", "creation_times", "last_access_times", "last_mod_times"
		};

		private static readonly string[] DbCapturesColumnsProperties = new string[]
		{
			"INTEGER PRIMARY KEY AUTOINCREMENT", "TEXT", "TEXT", "INTEGER", "INTEGER",
			"INTEGER", "TEXT", "TEXT", "INTEGER", "INTEGER",
			"INTEGER", "TEXT",
			"INTEGER", "TEXT", "INTEGER", "INTEGER",
			"INTEGER",
			"BLOB", "BLOB", "BLOB", "BLOB", "BLOB"
		};

		private static string _createCapturesTableSqlCommand;
		private static string _addCaptureSqlCommand;
		private static string _updateCaptureSqlCommand;

		private StringBuilder stringBuilder;
		private SQLiteConnection _connectionCaptures, _connectionRegister;
		private SQLiteCommand _commandCaptures, _commandRegister;
		string sqlCommand;
		
		public bool IsConnectionSuccessful = false;

		public bool IsConnected
		{
			get
			{
				if (_connectionCaptures == null)
				{
					return false;
				}
				return _connectionCaptures.State != ConnectionState.Closed && _connectionCaptures.State != ConnectionState.Broken;
			}
		}

		public SQLiteDbManager()
		{
			PrepFile(dbPath, dbCapturesFile);

			//constructing sql commands
			stringBuilder = new StringBuilder();
			_createCapturesTableSqlCommand = ConstructCreateSQLCommand(IDbManager.DbCapturesTableName, DbCapturesColumns, DbCapturesColumnsProperties);
			ConstructAddCaptureSQLCommand();
			ConstructUpdateCaptureSQLCommand();

			_connectionCaptures = CreateConnection(dbCapturesFilePath);

			try
			{
				_connectionCaptures.Open();

				LogDbActionWithStatus($"{GetLocalizedString("db_connect_suc")}{dbCapturesFilePath}",
					$"Successfully connected to db.:{dbCapturesFilePath}.");
			}
			catch (Exception e)
			{

			}

			_connectionRegister = CreateConnection(dbRegisterFilePath);

			try
			{
				_connectionRegister.Open();

				LogDbActionWithStatus($"{GetLocalizedString("db_connect_suc")}{dbRegisterFilePath}",
					$"Successfully connected to db.:{dbRegisterFilePath}.");
			}
			catch (Exception e)
			{

			}

			IsConnectionSuccessful = true;
			_commandCaptures = _connectionCaptures.CreateCommand();
			_commandRegister = _connectionRegister.CreateCommand();
			CreateTableIfNotFound(_commandCaptures, IDbManager.DbCapturesTableName, _createCapturesTableSqlCommand, dbCapturesFilePath);
		}

		~SQLiteDbManager()
		{
			if (_connectionCaptures.State != ConnectionState.Closed)
			{
				CloseConnection();
			}
		}

		private SQLiteConnection CreateConnection(string filePath)
		{
			return new SQLiteConnection("Data Source=" + filePath + ";Version=3;");
		}

		private void PrepFile(string path, string filename)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
				LogDbActionWithStatus(
					$"{GetLocalizedString("db_dir_create_suc")}{path}",
					$"Successfully created db. directory:{path}.");
			}

			string filePath = path + filename;

			if (!File.Exists(filePath))
			{
				SQLiteConnection.CreateFile(filePath);
				LogDbActionWithStatus(
					$"{GetLocalizedString("db_file_create_suc")}{filePath}",
					$"Successfully created db. file:{filePath}.");
			}
		}

		private string ConstructCreateSQLCommand(string tableName, string[] colNames, string[] colProperties)
		{
			stringBuilder.Clear();
			stringBuilder.Append("CREATE TABLE '").Append(tableName).Append("'(");
			int i;
			for (i = 0; i < colNames.Length-1; i++)
			{
				stringBuilder.Append(colNames[i]).Append(" ").Append(colProperties[i])
					.Append(", ");
			}
			stringBuilder.Append(colNames[i]).Append(" ").Append(colProperties[i]).Append(")");
			
			return stringBuilder.ToString();
		}

		private void ConstructAddCaptureSQLCommand()
		{
			_addCaptureSqlCommand = "insert into " + IDbManager.DbCapturesTableName + " (" + String.Join(", ", DbCapturesColumnsWithoutId) +
			                       ") values (@" + String.Join(", @", DbCapturesColumnsWithoutId) + ")";
		}

		private void ConstructUpdateCaptureSQLCommand()
		{
			stringBuilder.Clear();
			stringBuilder.Append("update ").Append(IDbManager.DbCapturesTableName).Append(" set ");
			int i;
			for (i = 0; i < DbCapturesColumnsWithoutId.Length-1; i++)
			{
				stringBuilder.Append(DbCapturesColumnsWithoutId[i]).Append(" = @")
					.Append(DbCapturesColumnsWithoutId[i]).Append(", ");
			}

			stringBuilder.Append(DbCapturesColumnsWithoutId[i]).Append(" = @")
				.Append(DbCapturesColumnsWithoutId[i]);

			stringBuilder.Append(" where ").Append(DbCapturesColumns[0]).Append(" = @").Append(DbCapturesColumns[0]);
			_updateCaptureSqlCommand = stringBuilder.ToString();
		}

		public List<Capture> GetCaptures()
		{
			IEnumerable<Capture> ret = null;

			try
			{
				ret = _connectionCaptures.Query<Capture>("select * from " + IDbManager.DbCapturesTableName, new DynamicParameters());

				LogDbActionWithStatus($"{GetLocalizedString("db_list_suc")} {IDbManager.DbCapturesTableName}",
					$"Successfully loaded captures from: {dbCapturesFilePath}.");

			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_list_fail")} {IDbManager.DbCapturesTableName}",
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
				_connectionCaptures.Execute(_addCaptureSqlCommand, capture);

				LogDbActionWithStatus($"{GetLocalizedString("db_save_suc")} {IDbManager.DbCapturesTableName}",
					$"Successfully saved a capture to: {dbCapturesFilePath}.{LineSeparator}" +
					$"({nameof(capture.drive_model)} = {capture.drive_model}, {nameof(capture.drive_sn)} = {capture.drive_sn})");

			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_save_fail")} {IDbManager.DbCapturesTableName}",
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
				_connectionCaptures.Execute(_updateCaptureSqlCommand, capture);

				LogDbActionWithStatus($"{GetLocalizedString("db_update_suc")} {IDbManager.DbCapturesTableName}",
					$"Successfully updated a capture in: {dbCapturesFilePath}. (id = {capture.id})");

			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_update_fail")} {IDbManager.DbCapturesTableName}",
					$"Failed to update a capture in: {dbCapturesFilePath}. (id = {capture.id}){LineSeparator}" +
					$"{e.Message}",
					ERROR);

			}
		}

		public void DeleteCapture(Capture capture)
		{
			try
			{
				_connectionCaptures.Execute("delete from " + IDbManager.DbCapturesTableName +
									 " where id = @id and drive_model = @drive_model and drive_sn = @drive_sn and volume_guid = @volume_guid", capture);

				LogDbActionWithStatus($"{GetLocalizedString("db_delete_suc")} {IDbManager.DbCapturesTableName}",
					$"Successfully deleted a capture from: {dbCapturesFilePath}. (id = {capture.id})");

			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_delete_fail")} {IDbManager.DbCapturesTableName}",
					$"Failed to delete a capture from: {dbCapturesFilePath}. (id = {capture.id}){LineSeparator}" +
					$"{e.Message}",
					ERROR);

			}
		}

		public bool DeleteAllCaptures()
		{
			try
			{
				_connectionCaptures.Execute("delete from " + IDbManager.DbCapturesTableName);

				LogDbActionWithStatus($"{GetLocalizedString("db_delete_all_suc")} {GetLocalizedString(ConfigNameOfLocalDS)}",
					$"Successfully deleted all captures from: {dbCapturesFilePath}.");
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_delete_all_fail")} {GetLocalizedString(ConfigNameOfLocalDS)}",
					$"Failed to delete all captures from: {dbCapturesFilePath}.{LineSeparator}" +
					$"{e.Message}",
					ERROR);

				return false;
			}

			return true;
		}

		public void CloseConnection()
		{
			_connectionCaptures.Close();

			LogDbActionWithStatus($"{GetLocalizedString("db_connect_closed")} {IDbManager.DbCapturesTableName}",
				$"Closed connection to: {dbCapturesFilePath}.");
		}

		public bool ConnectAndCreateTable()
		{
			return IsConnected;
		}

		//returns whether the operation completed successfully
		//implicitly adds an id column
		public bool CreateTable(string tableName, List<ColumnInfo> columns)
		{
			PrepFile(dbPath, dbRegisterFile);

			List<string> colNames = new List<string>();
			List<string> colProp = new List<string>();

			colNames.Add(DbCapturesColumns[0]);
			colProp.Add(DbCapturesColumnsProperties[0]);

			foreach (var columnInfo in columns)
			{
				colNames.Add(columnInfo.Name);
				colProp.Add(ColTypes[columnInfo.Type]);
			}

			string createTableCmd = ConstructCreateSQLCommand(tableName, colNames.ToArray(), colProp.ToArray());

			return CreateTableIfNotFound(_commandRegister, tableName, createTableCmd, dbRegisterFilePath);
		}

		//returns whether the operation completed successfully
		private bool CreateTableIfNotFound(SQLiteCommand command, string tableName, string createCommand, string dbFilePath)
		{
			if (!CheckIfTableExists(command, tableName))
			{
				command.CommandText = createCommand;
				try
				{
					int res = command.ExecuteNonQuery();
					LogDbActionWithStatus(
						$"{GetLocalizedString("db_table_create_suc")}{dbFilePath}/{tableName}",
						$"Successfully created table:{dbFilePath}/{tableName}.");
				}
				catch (Exception e)
				{
					LogDbActionWithMsgBox($"{GetLocalizedString("db_table_create_fail")} ",
						$"Failed to create table: {dbFilePath}/{tableName}.",
						GetLocalizedString("db_table_create_fail_title"), MessageBoxImage.Error, e.Message);

					return false;
				}
			}
			else
			{
				LogDbActionWithStatus(
					$"{GetLocalizedString("db_table_found")}{tableName}{GetLocalizedString("db_table_found2")}{dbFilePath}",
					$"Found table '{tableName}' in database:{dbFilePath}.");

				return true;
			}

			return true;
		}

		private bool CheckIfTableExists(SQLiteCommand command, string tableName)
		{
			command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='" + tableName +
			                      "'";
			var res = command.ExecuteScalar();

			return res != null && res.ToString() == IDbManager.DbCapturesTableName;
		}
	}
}
