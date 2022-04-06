using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using ColdStorageManager.Models;
using Dapper;
using MySql.Data.MySqlClient;
using static ColdStorageManager.Globals;
using static ColdStorageManager.Logger;

namespace ColdStorageManager.DBManagers
{
	public class MySQLDbManager : IDbManager
	{
		public MySQLDbConnectionProfile Profile { get; set; }

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

		private static string CreateCapturesTableSQLCommand;
		private static string AddCaptureSQLCommand;
		private static string UpdateCaptureSQLCommand;

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

		public MySQLDbManager(MySQLDbConnectionProfile connectionProfile, Window msgBoxOwner = null)
		{
			
			Profile = connectionProfile;
			MsgBoxOwner = msgBoxOwner;
		}

		~MySQLDbManager()
		{
			// Console.WriteLine("desTRUCT!");
			if (_connection.State != ConnectionState.Closed)
			{
				CloseConnection();
			}
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

		public bool TestConnection()
		{
			return CreateConnection(true);
		}

		public bool ConnectAndCreateTableIfNotFound()
		{
			bool ret = false;
			if (ret = CreateConnection())
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

		private bool CreateConnection(bool isTest = false)
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
					LogDbActionSameMsg(GetLocalizedString("success_connection") + Profile.HostWithPort + ".");
				}
			}
			catch (MySqlException e)
			{
				LogDbActionWithMsgBoxWithOwner(GetLocalizedString("error_connection") + Profile.HostWithPort + ".",
					GetLocalizedString("error_connection_title"),
					MessageBoxImage.Error,
					MsgBoxOwner,
						e.Message);
				return false;
			}
			catch(AggregateException e)
			{
				LogDbActionWithMsgBoxWithOwner(GetLocalizedString("error_connection") + Profile.HostWithPort + ".",
					GetLocalizedString("error_connection_title"),
					MessageBoxImage.Error,
					MsgBoxOwner,
					e.Message);
				return false;
			}

			return true;
		}

		public List<Capture> GetCaptures()
		{
			IEnumerable<Capture> ret = null;

			try
			{
				ret = _connection.Query<Capture>("select * from " + IDbManager.DbCapturesTableName, new DynamicParameters());

				LogDbAction($"{GetLocalizedString("db_list_suc")} {Profile.ProfileName}",
					$"Successfully loaded captures from: {Profile.PathToTable}.");

			}
			catch (Exception e)
			{
				LogDbAction($"{GetLocalizedString("db_list_fail")} {Profile.ProfileName}",
					$"Failed to load captures from: {Profile.PathToTable}.{LineSeparator}" +
					$"{e.Message}",
					ERROR);

			}

			return ret.ToList();
		}

		public void SaveCapture(Capture capture)
		{
			try
			{
				_connection.Execute(AddCaptureSQLCommand, capture);

				LogDbAction($"{GetLocalizedString("db_save_suc")} {Profile.ProfileName}",
					$"Successfully saved a capture to: {Profile.PathToTable}.{LineSeparator}" +
					$"({nameof(capture.drive_model)} = {capture.drive_model}, {nameof(capture.drive_sn)} = {capture.drive_sn})");

			}
			catch (Exception e)
			{
				LogDbAction($"{GetLocalizedString("db_save_fail")} {Profile.ProfileName}",
					$"Failed to save a capture to: {Profile.PathToTable}.{LineSeparator}" +
					$"({nameof(capture.drive_model)} = {capture.drive_model}, {nameof(capture.drive_sn)} = {capture.drive_sn}){LineSeparator}" +
					$"{e.Message}",
					ERROR);

			}

		}

		public void UpdateCaptureById(Capture capture)
		{
			try
			{
				_connection.Execute(UpdateCaptureSQLCommand, capture);

				LogDbAction($"{GetLocalizedString("db_update_suc")} {Profile.ProfileName}",
					$"Successfully updated a capture in: {Profile.PathToTable}. (id = {capture.id})");

			}
			catch (Exception e)
			{
				LogDbAction($"{GetLocalizedString("db_update_fail")} {Profile.ProfileName}",
					$"Failed to update a capture in: {Profile.PathToTable}. (id = {capture.id}){LineSeparator}" +
					$"{e.Message}",
					ERROR);

			}
		}

		public void DeleteCapture(Capture capture)
		{
			try
			{
				_connection.Execute("delete from " + IDbManager.DbCapturesTableName +
									" where drive_model = @drive_model and drive_sn = @drive_sn and volume_guid = @volume_guid", capture);
				
				LogDbAction($"{GetLocalizedString("db_delete_suc")} {Profile.ProfileName}",
					$"Successfully deleted a capture from: {Profile.PathToTable}. (id = {capture.id})");
			}
			catch (Exception e)
			{
				LogDbAction($"{GetLocalizedString("db_delete_fail")} {Profile.ProfileName}",
					$"Failed to delete a capture from: {Profile.PathToTable}. (id = {capture.id}){LineSeparator}" +
					$"{e.Message}",
					ERROR);
			}

		}

		public void CloseConnection()
		{
			if (_connection.State != ConnectionState.Closed)
			{
				_connection.Close();

				LogDbAction($"{GetLocalizedString("db_connect_closed")} {Profile.ProfileName}",
					$"Closed connection to: {Profile.PathToDb}.");
			}
			else
			{
				LogDbAction($"{GetLocalizedString("db_connect_already_closed")} {Profile.ProfileName}",
					$"Tried to close already closed connection: {Profile.PathToDb}.");
			}
		}

		private bool CreateTableIfNotFound(string tableName)
		{
			if (!checkIfTableExists(tableName))
			{
				_command.CommandText = CreateCapturesTableSQLCommand;
				try
				{
					int res = _command.ExecuteNonQuery();
					LogDbAction(
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
				LogDbAction(
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
