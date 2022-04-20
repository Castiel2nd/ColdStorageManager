using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Navigation;
using ColdStorageManager.Models;
using Dapper;
using MySql.Data.MySqlClient;
using static ColdStorageManager.Globals;
using static ColdStorageManager.Logger;

namespace ColdStorageManager.DBManagers
{
	public class MySQLDbManager : IDbManager
	{
		public DbConnectionProfile Profile { get; set; }

		public bool IsConnected
		{
			get
			{
				if (_connection == null)
				{
					return false;
				}
				return _connection.State != ConnectionState.Closed && _connection.State != ConnectionState.Broken;
			}
		}

		private MySqlConnection _connection;
		private MySqlCommand _command;
		private string _connectionString = "server={0};port={1};database={2};uid={3};pwd={4};";
		private bool _isInitialized = false;

		private StringBuilder stringBuilder;

		public Window MsgBoxOwner
		{
			get
			{
				return (_msgBoxOwner == null ? mainWindow : _msgBoxOwner);
			}
			set
			{
				_msgBoxOwner = value;
			}
		}

		private Window _msgBoxOwner;

		private static string CreateCapturesTableSQLCommand;
		private static string AddCaptureSQLCommand;
		private static string UpdateCaptureSQLCommand;

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
			"INTEGER PRIMARY KEY AUTO_INCREMENT", "TEXT", "TEXT", "INTEGER", "INTEGER",
			"BIGINT", "TEXT", "TEXT", "INTEGER", "BIGINT",
			"BIGINT", "TEXT",
			"INTEGER", "TEXT", "INTEGER", "INTEGER",
			"INTEGER",
			"LONGBLOB", "LONGBLOB", "LONGBLOB", "LONGBLOB", "LONGBLOB"
		};

		public MySQLDbManager(DbConnectionProfile connectionProfile, Window msgBoxOwner = null)
		{
			
			Profile = connectionProfile;
			MsgBoxOwner = msgBoxOwner;
		}

		~MySQLDbManager()
		{
			// Console.WriteLine("desTRUCT!");
			CloseConnection();
		}

		private void ConstructCreateSQLCommand()
		{
			stringBuilder.Clear();
			stringBuilder.Append("CREATE TABLE ").Append(IDbManager.DbCapturesTableName).Append("(");
			int i;
			for (i = 0; i < dbCapturesColumns.Length - 1; i++)
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
			for (i = 0; i < dbCapturesColumnsWithoutId.Length - 1; i++)
			{
				stringBuilder.Append(dbCapturesColumnsWithoutId[i]).Append(" = @")
					.Append(dbCapturesColumnsWithoutId[i]).Append(", ");
			}

			stringBuilder.Append(dbCapturesColumnsWithoutId[i]).Append(" = @")
				.Append(dbCapturesColumnsWithoutId[i]);

			stringBuilder.Append(" where ").Append(dbCapturesColumns[0]).Append(" = @").Append(dbCapturesColumns[0]);
			UpdateCaptureSQLCommand = stringBuilder.ToString();
		}

		// checks if the connection is open, and if not, it tries opening it.
		// return if the connection is open at the end of the function
		private bool CheckConnection()
		{
			if (_isInitialized)
			{
				if (IsConnected)
				{
					return true;
				}
				else
				{
					return Connect();
				}
			}
			else
			{
				_isInitialized = true;
				return ConnectAndCreateTable();
			}
		}

		public bool TestConnection()
		{
			return Connect(true);
		}

		public bool ConnectAndCreateTable()
		{
			bool ret = false;
			if (ret = Connect())
			{
				//constructing sql commands
				stringBuilder = new StringBuilder();
				ConstructCreateSQLCommand();
				ConstructAddCaptureSQLCommand();
				ConstructUpdateCaptureSQLCommand();

				_command = _connection.CreateCommand();
				ret = CreateTableIfNotFound(IDbManager.DbCapturesTableName);
			}

			return ret;
		}

		public bool CreateTable(string tableName, List<ColumnInfo> columns)
		{
			throw new NotImplementedException();
		}

		public List<string> GetTableNames()
		{
			throw new NotImplementedException();
		}

		public ColumnInfo[] GetColumns(string tableName)
		{
			throw new NotImplementedException();
		}

		public bool SetTableData(ref TableModel table)
		{
			throw new NotImplementedException();
		}

		public bool SetCellData(string tableName, string columnName, int rowId, object data)
		{
			throw new NotImplementedException();
		}

		public long GetLastInsertedRowId()
		{
			throw new NotImplementedException();
		}

		public object[] GetLastInsertedRow(string tableName, ColumnInfo[] columns)
		{
			throw new NotImplementedException();
		}

		public object[] GetRowById(string tableName, ColumnInfo[] columns, long rowId)
		{
			throw new NotImplementedException();
		}

		public bool DeleteRowById(string tableName, long rowId)
		{
			throw new NotImplementedException();
		}


		// returns whether the connection is open at the end of this function
		private bool Connect(bool isTest = false)
		{
			if (_connection == null)
			{
				_connectionString = string.Format(_connectionString, Profile.Host, Profile.Port,
					Profile.DatabaseName, Profile.UID, Profile.Password);

				try
				{
					_connection = new MySqlConnection(_connectionString);
					_connection.Open();

					if (isTest)
					{
						LogDbActionWithMsgBoxWithOwner(GetLocalizedString("success_connection") + Profile.HostWithPort + ".",
							GetLocalizedString("success_connection_title"),
							MessageBoxImage.Information,
							MsgBoxOwner);
						CloseConnection();
					}
					else
					{
						LogDbActionWithStatusSameMsg(GetLocalizedString("success_connection") + Profile.HostWithPort + ".");
					}
				}
				catch (Exception e)
				{
					if (e is MySqlException ||
					    e is AggregateException)
					{
						LogDbActionWithMsgBoxWithOwner(GetLocalizedString("error_connection") + Profile.HostWithPort + ".",
							GetLocalizedString("error_connection_title"),
							MessageBoxImage.Error,
							MsgBoxOwner,
							e.Message);
					}

					return false;
				}

				return true;
			}
			else if (_connection.State == ConnectionState.Open)
			{
				LogDbAction($"Tried opening already open connection. ({Profile.ProfileName})", WARNING);

				return true;
			}else if (_connection.State == ConnectionState.Closed || _connection.State == ConnectionState.Broken)
			{
				try
				{
					_connection.Open();

					if (isTest)
					{
						LogDbActionWithMsgBoxWithOwner(GetLocalizedString("success_connection") + Profile.HostWithPort + ".",
							GetLocalizedString("success_connection_title"),
							MessageBoxImage.Information,
							MsgBoxOwner);
						CloseConnection();
					}
					else
					{
						LogDbActionWithStatusSameMsg(GetLocalizedString("success_connection") + Profile.HostWithPort + ".");
					}
				}
				catch (Exception e)
				{
					if (e is MySqlException ||
					    e is AggregateException)
					{
						LogDbActionWithMsgBoxWithOwner(GetLocalizedString("error_connection") + Profile.HostWithPort + ".",
							GetLocalizedString("error_connection_title"),
							MessageBoxImage.Error,
							MsgBoxOwner,
							e.Message);
					}

					return false;
				}
			}
			else
			{
				LogDbAction($"Tried opening busy connection. ({Profile.ProfileName}, State = {_connection.State})", WARNING);

				return true;
			}

			return _connection.State == ConnectionState.Open;
		}

		public List<Capture> GetCaptures()
		{
			if (!CheckConnection())
			{
				return null;
			}

			IEnumerable<Capture> ret = null;

			try
			{
				ret = _connection.Query<Capture>("select * from " + IDbManager.DbCapturesTableName, new DynamicParameters());

				LogDbActionWithStatus($"{GetLocalizedString("db_list_suc")} {Profile.ProfileName}",
					$"Successfully loaded captures from: {Profile.PathToTable}.");
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_list_fail")} {Profile.ProfileName}",
					$"Failed to load captures from: {Profile.PathToTable}.{LineSeparator}" +
					$"{e.Message}",
					ERROR);
			}

			if (ret != null) return ret.ToList();
			return null;
		}

		public void SaveCapture(Capture capture)
		{
			if (!CheckConnection())
			{
				return;
			}

			try
			{
				_connection.Execute(AddCaptureSQLCommand, capture);

				LogDbActionWithStatus($"{GetLocalizedString("db_save_suc")} {Profile.ProfileName}",
					$"Successfully saved a capture to: {Profile.PathToTable}.{LineSeparator}" +
					$"({nameof(capture.drive_model)} = {capture.drive_model}, {nameof(capture.drive_sn)} = {capture.drive_sn})");

			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_save_fail")} {Profile.ProfileName}",
					$"Failed to save a capture to: {Profile.PathToTable}.{LineSeparator}" +
					$"({nameof(capture.drive_model)} = {capture.drive_model}, {nameof(capture.drive_sn)} = {capture.drive_sn}){LineSeparator}" +
					$"{e.Message}",
					ERROR);

			}

		}

		public void UpdateCaptureById(Capture capture)
		{
			if (!CheckConnection())
			{
				return;
			}

			try
			{
				_connection.Execute(UpdateCaptureSQLCommand, capture);

				LogDbActionWithStatus($"{GetLocalizedString("db_update_suc")} {Profile.ProfileName}",
					$"Successfully updated a capture in: {Profile.PathToTable}. (id = {capture.id})");

			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_update_fail")} {Profile.ProfileName}",
					$"Failed to update a capture in: {Profile.PathToTable}. (id = {capture.id}){LineSeparator}" +
					$"{e.Message}",
					ERROR);

			}
		}

		public void DeleteCapture(Capture capture)
		{
			if (!CheckConnection())
			{
				return;
			}

			try
			{
				_connection.Execute("delete from " + IDbManager.DbCapturesTableName +
									" where id = @id and drive_model = @drive_model and drive_sn = @drive_sn and volume_guid = @volume_guid", capture);
				
				LogDbActionWithStatus($"{GetLocalizedString("db_delete_suc")} {Profile.ProfileName}",
					$"Successfully deleted a capture from: {Profile.PathToTable}. (id = {capture.id})");
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_delete_fail")} {Profile.ProfileName}",
					$"Failed to delete a capture from: {Profile.PathToTable}. (id = {capture.id}){LineSeparator}" +
					$"{e.Message}",
					ERROR);
			}

		}

		public bool DeleteAllCaptures()
		{
			if (!CheckConnection())
			{
				return false;
			}

			try
			{
				_connection.Execute("delete from " + IDbManager.DbCapturesTableName);

				LogDbActionWithStatus($"{GetLocalizedString("db_delete_all_suc")} {Profile.ProfileName}",
					$"Successfully deleted all captures from: {Profile.PathToTable}.");
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_delete_all_fail")} {Profile.ProfileName}",
					$"Failed to delete all captures from: {Profile.PathToTable}.{LineSeparator}" +
					$"{e.Message}",
					ERROR);

				return false;
			}

			return true;
		}

		public void CloseConnection()
		{
			if (_connection != null)
			{
				if (_connection.State != ConnectionState.Closed)
				{
					_connection.Close();

					LogDbActionWithStatus($"{GetLocalizedString("db_connect_closed")} {Profile.ProfileName}",
						$"Closed connection to: {Profile.PathToDb}.");

					return;
				}
			}
			LogDbActionWithStatus($"{GetLocalizedString("db_connect_already_closed")} {Profile.ProfileName}",
				$"Tried to close already closed connection: {Profile.PathToDb}.");
		}

		private bool CreateTableIfNotFound(string tableName)
		{
			if (!checkIfTableExists(tableName))
			{
				_command.CommandText = CreateCapturesTableSQLCommand;
				try
				{
					int res = _command.ExecuteNonQuery();
					LogDbActionWithStatus(
						$"{GetLocalizedString("db_table_create_suc")}{Profile.PathToTable}",
						$"Successfully created table: {Profile.PathToTable}.");
				}
				catch (Exception e)
				{
					LogDbActionWithMsgBox(GetLocalizedString("db_table_create_fail") + Profile.PathToTable,
						$"Failed to create table: {Profile.PathToTable}.",
						GetLocalizedString("db_table_create_fail_title"), MessageBoxImage.Error, e.Message);
					return false;
				}
			}
			else
			{
				LogDbActionWithStatus(
					$"{GetLocalizedString("db_table_found")}{IDbManager.DbCapturesTableName}{GetLocalizedString("db_table_found2")}{Profile.PathToDb}.",
					$"Found table '{IDbManager.DbCapturesTableName}' in database:{Profile.PathToDb}.");
			}

			return true;
		}

		private bool checkIfTableExists(string tableName)
		{
			_command.CommandText =
				$"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Profile.DatabaseName}' AND table_name = '{IDbManager.DbCapturesTableName}' LIMIT 1";
			var res = _command.ExecuteScalar();

			return res != null;
		}
	}
}
