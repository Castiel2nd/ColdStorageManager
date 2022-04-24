using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

		private StringBuilder stringBuilder = new StringBuilder();

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

		private const string GetTableNamesSql = "SELECT table_name FROM information_schema.tables WHERE table_schema = @DbName";
		private const string GetTableNamesSql2 = "SHOW tables";

		private static readonly Dictionary<CSMColumnType, string> ColTypes = new Dictionary<CSMColumnType, string>(){ {CSMColumnType.Text, "TEXT"},
			{CSMColumnType.Integer, "INTEGER"} };

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

		private string ConstructCreateSQLCommand(string tableName, string[] colNames, string[] colProperties)
		{
			stringBuilder.Clear();
			stringBuilder.Append("CREATE TABLE ").Append(tableName).Append("(");
			int i;
			for (i = 0; i < colNames.Length - 1; i++)
			{
				stringBuilder.Append("`").Append(colNames[i]).Append("` ").Append(colProperties[i])
					.Append(", ");
			}
			stringBuilder.Append("`").Append(colNames[i]).Append("` ").Append(colProperties[i]).Append(")");

			return stringBuilder.ToString();
		}

		private void ConstructAddCaptureSQLCommand()
		{
			AddCaptureSQLCommand = "insert into " + IDbManager.DbCapturesTableName + " (" + String.Join(", ", DbCapturesColumnsWithoutId) +
			                       ") values (@" + String.Join(", @", DbCapturesColumnsWithoutId) + ")";
		}

		private void ConstructUpdateCaptureSQLCommand()
		{
			stringBuilder.Clear();
			stringBuilder.Append("update ").Append(IDbManager.DbCapturesTableName).Append(" set ");
			int i;
			for (i = 0; i < DbCapturesColumnsWithoutId.Length - 1; i++)
			{
				stringBuilder.Append(DbCapturesColumnsWithoutId[i]).Append(" = @")
					.Append(DbCapturesColumnsWithoutId[i]).Append(", ");
			}

			stringBuilder.Append(DbCapturesColumnsWithoutId[i]).Append(" = @")
				.Append(DbCapturesColumnsWithoutId[i]);

			stringBuilder.Append(" where ").Append(DbCapturesColumns[0]).Append(" = @").Append(DbCapturesColumns[0]);
			UpdateCaptureSQLCommand = stringBuilder.ToString();
		}

		// checks if the connection is open, and if not, it tries opening it.
		// returns if the connection is open at the end of the function
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
			if (Connect())
			{
				//constructing sql commands
				stringBuilder = new StringBuilder();
				CreateCapturesTableSQLCommand = ConstructCreateSQLCommand(IDbManager.DbCapturesTableName, DbCapturesColumns, DbCapturesColumnsProperties);
				ConstructAddCaptureSQLCommand();
				ConstructUpdateCaptureSQLCommand();

				_command = _connection.CreateCommand();
				ret = CreateTableIfNotFound(IDbManager.DbCapturesTableName, CreateCapturesTableSQLCommand);
			}

			return ret;
		}

		public bool CreateTable(string tableName, List<ColumnInfo> columns)
		{
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

			return CreateTableIfNotFound(tableName, createTableCmd);
		}

		public List<string> GetTableNames()
		{
			List<string> tableNames = null;

			try
			{
				var res = _connection.Query(GetTableNamesSql, new{DbName = Profile.DatabaseName});

				if (debugLogging)
				{
					LogDbActionWithStatus($"{GetLocalizedString("db_get_table_names_suc")} {Profile.PathToTable}",
						$"Successfully loaded tables from {Profile.PathToTable}. Result = {res}, Count = {res.Count()}");
				}

				tableNames = new List<string>();

				string tableName;

				foreach (dynamic row in res)
				{
					tableName = row.table_name;

					if (tableName.Equals("captures", StringComparison.InvariantCultureIgnoreCase))
						continue;

					tableNames.Add(tableName);
				}
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_get_table_names_fail")} {Profile.ProfileName}",
					$"Failed to get table names from {Profile.PathToTable}.{LineSeparator}" +
					$"Error message: {e.Message}", ERROR);
			}

			return tableNames;
		}

		public ColumnInfo[] GetColumns(string tableName)
		{
			List<ColumnInfo> columnInfos = new List<ColumnInfo>();

			try
			{
				var res = _connection.Query($"SHOW COLUMNS FROM {tableName}");

				if (debugLogging)
				{
					LogDbActionWithStatus($"{GetLocalizedString("db_get_table_cols_suc")} {tableName}",
						$"Successfully loaded columns of table {tableName}. Source = {_connection.DataSource}, Result = {res}, Count = {res.Count()}");
				}

				foreach (dynamic row in res)
				{
					ColumnInfo columnInfo = new ColumnInfo();
					// Console.WriteLine($"found column: name: {row.name} type: {row.type}{LineSeparator}");
					switch (row.Type as string)
					{
						case "text":
							columnInfo.Type = CSMColumnType.Text;
							break;
						case { } s when s.StartsWith("int") ||
											  s.StartsWith("bigint"):
							columnInfo.Type = CSMColumnType.Integer;
							break;
						default:
							if (debugLogging)
							{
								LogDbAction($"Unsupported column type ({row.Type as string}) found in table {tableName}, at {Profile.PathToDb}.", WARNING);
							}
							columnInfo.Type = CSMColumnType.NullType;
							break;
					}

					columnInfo.Name = row.Field;

					if (columnInfo.Name.Equals("id"))
					{
						columnInfo.IsTextEditable = false;
					}

					columnInfos.Add(columnInfo);
				}
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_get_table_cols_fail")} {tableName}",
					$"Failed to get columns from table '{tableName}'.{LineSeparator}" +
					$"Error message: {e.Message}", ERROR);
			}

			return columnInfos.ToArray();
		}

		public bool SetTableData(ref TableModel table)
		{
			bool ret = false;

			try
			{
				var res = _connection.Query($"SELECT * FROM {table.Name}");

				if (debugLogging)
				{
					LogDbActionWithStatus($"{GetLocalizedString("db_get_table_data_suc")} {table.Name}",
						$"Successfully loaded data from table {table.Name}. Source = {_connection.DataSource}, Result = {res}, Count = {res.Count()}");
				}

				List<object[]> data = new List<object[]>();

				foreach (dynamic dapperRow in res)
				{
					IDictionary<string, object> row = dapperRow as IDictionary<string, object>;

					// Console.WriteLine("row: " + row);

					object[] dataArray = new object[table.Columns.Length];

					for (int i = 0; i < table.Columns.Length; i++)
					{
						row.TryGetValue(table.Columns[i].Name, out dataArray[i]);
						// Console.WriteLine($"Column name: {table.Columns[i].Name}");
						// Console.WriteLine($"Column value: {dataArray[i]}");
					}

					data.Add(dataArray);
				}

				table.Data = new ObservableCollection<object[]>(data);

				ret = true;
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_get_table_data_fail")} {table.Name}",
					$"Failed to get data from table {table.Name}.{LineSeparator}" +
					$"Error message: {e.Message}", ERROR);
			}

			return ret;
		}

		public bool SetCellData(string tableName, string columnName, object rowId, object data)
		{
			if (rowId == null)
			{
				try
				{
					int affectedRows = _connection.Execute($"INSERT INTO {tableName} (`{columnName}`) VALUES (@Data)",
						new { Data = data });

					if (debugLogging)
					{
						LogDbActionWithStatus($"{GetLocalizedString("db_insert_row_suc")} {tableName}",
							$"Successfully inserted row into table {tableName}. Column = {columnName}, affectedRows = {affectedRows}");
					}

					return affectedRows == 1;
				}
				catch (Exception e)
				{
					LogDbActionWithStatus($"{GetLocalizedString("db_insert_row_fail")} {tableName}",
						$"Failed to add new record to '{tableName}' with '{columnName}' = '{data}'.{LineSeparator}" +
						$"Error message: {e.Message}", ERROR);
				}
			}
			else
			{
				try
				{
					int affectedRows = _connection.Execute($"UPDATE {tableName} SET `{columnName}` = @Data WHERE id = {rowId}",
						new { Data = data });

					if (debugLogging)
					{
						LogDbActionWithStatus($"{GetLocalizedString("db_update_row_suc")} {tableName}",
							$"Successfully updated row in table {tableName}. Column = {columnName}, id = {rowId}, affectedRows = {affectedRows}");
					}

					return affectedRows == 1;
				}
				catch (Exception e)
				{
					LogDbActionWithStatus($"{GetLocalizedString("db_update_row_fail")} {tableName}",
						$"Failed to update value of column '{columnName}' in table '{tableName}'.(id = {rowId}){LineSeparator}" +
						$"Error message: {e.Message}", ERROR);
				}
			}

			return false;
		}

		public ulong GetLastInsertedRowId()
		{
			ulong ret = UInt64.MaxValue;

			try
			{
				ret = (ulong)_connection.ExecuteScalar("SELECT LAST_INSERT_ID()");

				if (debugLogging)
				{
					LogDbActionWithStatus($"{GetLocalizedString("db_get_last_row_id_suc")} {_connection.DataSource}",
						$"Successfully loaded row from {_connection.DataSource}");
				}
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_get_last_row_id_fail")} {_connection.DataSource}",
					$"Failed to get last inserted row id from {_connection.DataSource}.{LineSeparator}" +
					$"Error message: {e.Message}", ERROR);
			}

			return ret;
		}

		public object[] GetLastInsertedRow(string tableName, ColumnInfo[] columns)
		{
			object[] ret = null;
			ulong rowId = GetLastInsertedRowId();

			if (rowId == UInt64.MaxValue)
			{

			}
			else if (rowId == 0)
			{
				LogDbAction($"Could not select last inserted row from {_connection.DataSource}, since there was none. (rowId = 0)", WARNING);
			}
			else
			{
				ret = GetRowById(tableName, columns, rowId);
			}

			return ret;
		}

		public object[] GetRowById(string tableName, ColumnInfo[] columns, object rowId)
		{
			object[] row = null;

			try
			{
				dynamic res = _connection.QueryFirst($"SELECT * FROM {tableName} WHERE id = @Id", new { Id = rowId });

				IDictionary<string, object> rowAsDict = res as IDictionary<string, object>;
				row = new object[columns.Length];

				for (int i = 0; i < columns.Length; i++)
				{
					rowAsDict.TryGetValue(columns[i].Name, out row[i]);
				}

				if (debugLogging)
				{
					LogDbActionWithStatus($"{GetLocalizedString("db_get_row_by_id_suc")} {tableName} (id = {rowId})",
						$"Successfully loaded row from {tableName}. (id = {rowId})");
				}
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_get_row_by_id_fail")} {tableName} (id = {rowId})",
					$"Failed to get row by id from {tableName}. (id = {rowId}){LineSeparator}" +
					$"Error message: {e.Message}", ERROR);
			}

			return row;
		}

		public bool DeleteRowById(string tableName, object rowId)
		{
			try
			{
				int rowsAffected = _connection.Execute($"DELETE FROM {tableName} WHERE id = @Id", new { Id = rowId });

				if (debugLogging)
				{
					LogDbActionWithStatus($"{GetLocalizedString("db_del_row_by_id_suc")} {tableName} (id = {rowId})",
						$"Successfully deleted row from table {tableName}. (id = {rowId})");
				}

				return rowsAffected == 1;
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_del_row_by_id_fail")} {tableName} (id = {rowId})",
					$"Failed to delete row by id from {tableName}. (id = {rowId}){LineSeparator}" +
					$"Error message: {e.Message}", ERROR);
			}

			return false;
		}

		public bool DeleteTable(string tableName)
		{
			try
			{
				_connection.Execute($"DROP TABLE {tableName}");

				LogDbActionWithStatus($"{GetLocalizedString("db_del_table_suc")} {tableName} {GetLocalizedString("from")} {_connection.DataSource}",
					$"Successfully deleted table {tableName} from {_connection.DataSource}.");


				return true;
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_del_table_fail")} {tableName} {GetLocalizedString("from")} {_connection.DataSource}",
					$"Failed to delete table {tableName} from {_connection.DataSource}.{LineSeparator}" +
					$"Error message: {e.Message}", ERROR);
			}

			return false;
		}

		public bool TryParse(CSMColumnType columnType, object data)
		{
			switch (columnType)
			{
				case CSMColumnType.Text:
					return true;
				case CSMColumnType.Integer:
					return int.TryParse(data.ToString(), out int res);
				case CSMColumnType.NullType:
					return false;
			}

			return false;
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

		private bool CreateTableIfNotFound(string tableName, string createTableCmd)
		{
			if (!CheckIfTableExists(tableName))
			{
				_command.CommandText = createTableCmd;
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
						$"Failed to create table: {Profile.PathToTable}.{LineSeparator}" +
						$"Error msg: {e.Message}{(debugLogging ? $"{LineSeparator} SQL: {createTableCmd}" : "")}",
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

		private bool CheckIfTableExists(string tableName)
		{
			_command.CommandText =
				$"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Profile.DatabaseName}' AND table_name = '{tableName}' LIMIT 1";
			var res = _command.ExecuteScalar();

			return res != null;
		}
	}
}
