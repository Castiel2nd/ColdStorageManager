using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

		private const string GetTableNamesSql = "SELECT tbl_name FROM sqlite_master WHERE type = 'table'";

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
			PrepFile(dbPath, dbRegisterFile);

			//constructing sql commands
			stringBuilder = new StringBuilder();
			_createCapturesTableSqlCommand = ConstructCreateSQLCommand(IDbManager.DbCapturesTableName, DbCapturesColumns, DbCapturesColumnsProperties);
			ConstructAddCaptureSQLCommand();
			ConstructUpdateCaptureSQLCommand();

			_connectionCaptures = CreateConnection(dbCapturesFilePath);

			try
			{
				_connectionCaptures.Open();

				LogDbActionWithStatus($"{GetLocalizedString("db_connect_suc")} {dbCapturesFilePath}",
					$"Successfully connected to db.:{dbCapturesFilePath}.");
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("error_connection")} {dbCapturesFilePath}",
					$"There was an error connecting to {dbCapturesFilePath}.{LineSeparator}" +
					$"Error message: {e.Message}", ERROR);
			}

			_connectionRegister = CreateConnection(dbRegisterFilePath);

			try
			{
				_connectionRegister.Open();

				LogDbActionWithStatus($"{GetLocalizedString("db_connect_suc")} {dbRegisterFilePath}",
					$"Successfully connected to db.:{dbRegisterFilePath}.");
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("error_connection")} {dbRegisterFilePath}",
					$"There was an error connecting to {dbRegisterFilePath}.{LineSeparator}" +
					$"Error message: {e.Message}", ERROR);
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
				stringBuilder.Append("\"").Append(colNames[i]).Append("\" ").Append(colProperties[i])
					.Append(", ");
			}
			stringBuilder.Append("\"").Append(colNames[i]).Append("\" ").Append(colProperties[i]).Append(")");
			
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

		private List<string> GetTableNamesFromConnection(SQLiteConnection conn, string filepath)
		{
			List<string> tableNames = null;

			try
			{
				var res = conn.Query(GetTableNamesSql);

				if (debugLogging)
				{
					LogDbActionWithStatus($"{GetLocalizedString("db_get_table_names_suc")} {filepath}",
						$"Successfully loaded tables from {filepath}. Result = {res}, Count = {res.Count()}");
				}

				tableNames = new List<string>();

				string tableName;

				foreach (dynamic row in res)
				{
					tableName = row.tbl_name;

					if(tableName.Equals("sqlite_sequence"))
						continue;

					tableNames.Add(tableName);
				}
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_get_table_names_fail")} {filepath}" ,
					$"Failed to get table names from {filepath}.{LineSeparator}" +
				            $"Error message: {e.Message}", ERROR);
			}

			return tableNames;
		}

		public List<string> GetTableNames()
		{
			return GetTableNamesFromConnection(_connectionRegister, dbRegisterFilePath);
		}

		private ColumnInfo[] GetColumnsFromConnection(SQLiteConnection connection, string tableName)
		{
			List<ColumnInfo> columnInfos = new List<ColumnInfo>();

			try
			{
				var res = connection.Query($"PRAGMA table_info({tableName})");

				if (debugLogging)
				{
					LogDbActionWithStatus($"{GetLocalizedString("db_get_table_cols_suc")} {tableName}",
						$"Successfully loaded columns of table {tableName}. Source = {connection.DataSource}, Result = {res}, Count = {res.Count()}");
				}

				foreach (dynamic row in res)
				{
					ColumnInfo columnInfo = new ColumnInfo();
					// Console.WriteLine($"found column: name: {row.name} type: {row.type}{LineSeparator}");
					switch (row.type as string)
					{
						case "TEXT":
							columnInfo.Type = CSMColumnType.Text;
							break;
						case "INTEGER":
							columnInfo.Type = CSMColumnType.Integer;
							break;
						default:
							if (debugLogging)
							{
								LogDbAction($"Unsupported column type ({row.Type as string}) found in table {tableName}, at {dbRegisterFilePath}.", WARNING);
							}
							columnInfo.Type = CSMColumnType.NullType;
							break;
					}

					columnInfo.Name = row.name;

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

		public ColumnInfo[] GetColumns(string tableName)
		{
			return GetColumnsFromConnection(_connectionRegister, tableName);
		}

		private bool SetTableDataFromConnection(SQLiteConnection connection, ref TableModel table)
		{
			bool ret = false;

			try
			{
				var res = connection.Query($"SELECT * FROM {table.Name}");

				if (debugLogging)
				{
					LogDbActionWithStatus($"{GetLocalizedString("db_get_table_data_suc")} {table.Name}",
						$"Successfully loaded data from table {table.Name}. Source = {connection.DataSource}, Result = {res}, Count = {res.Count()}");
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

		public bool SetTableData(ref TableModel table)
		{
			return SetTableDataFromConnection(_connectionRegister,ref table);
		}

		//a rowId of -1 indicates a new record that should be added 
		private bool SetCellDataViaConnection(SQLiteConnection connection, string tableName, string columnName,
			object rowId, object data)
		{
			if (rowId == null)
			{
				try
				{
					int affectedRows = connection.Execute($"INSERT INTO {tableName} (\"{columnName}\") VALUES (@Data)",
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
					int affectedRows = connection.Execute($"UPDATE {tableName} SET \"{columnName}\" = @Data WHERE id = {rowId}",
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

		public bool SetCellData(string tableName, string columnName, object rowId, object data)
		{
			return SetCellDataViaConnection(_connectionRegister, tableName, columnName, rowId, data);
		}

		//returns -1 if the query didn't complete, 0 if there was no previously inserted row in the current session
		private ulong GetLastInsertedRowIdViaConnection(SQLiteConnection connection)
		{
			ulong ret = UInt64.MaxValue;

			try
			{
				ret = MapLongToUlong((long)connection.ExecuteScalar("SELECT last_insert_rowid()"));

				if (debugLogging)
				{
					LogDbActionWithStatus($"{GetLocalizedString("db_get_last_row_id_suc")} {connection.DataSource}",
						$"Successfully loaded row from {connection.DataSource}");
				}
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_get_last_row_id_fail")} {connection.DataSource}",
					$"Failed to get last inserted row id from {connection.DataSource}.{LineSeparator}" +
				            $"Error message: {e.Message}", ERROR);
			}

			return ret;
		}

		public ulong GetLastInsertedRowId()
		{
			return GetLastInsertedRowIdViaConnection(_connectionRegister);
		}

		//returns null if it failed
		private object[] GetLastInsertedRowViaConnection(SQLiteConnection connection, string tableName,
			ColumnInfo[] columns)
		{
			object[] ret = null;
			ulong rowId = GetLastInsertedRowIdViaConnection(connection);

			if (rowId == UInt64.MaxValue)
			{

			}else if (rowId == 0)
			{
				LogDbAction($"Could not select last inserted row from {connection.DataSource}, since there was none. (rowId = 0)", WARNING);
			}
			else
			{
				ret = GetRowByIdViaConnection(connection, tableName, columns, rowId);
			}

			return ret;
		}

		public object[] GetLastInsertedRow(string tableName, ColumnInfo[] columns)
		{
			return GetLastInsertedRowViaConnection(_connectionRegister, tableName, columns);
		}

		//returns null of row was not found
		private object[] GetRowByIdViaConnection(SQLiteConnection connection, string tableName, ColumnInfo[] columns,
			object rowId)
		{
			object[] row = null;

			try
			{
				dynamic res = connection.QueryFirst($"SELECT * FROM {tableName} WHERE id = @Id", new { Id = rowId });

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

		public object[] GetRowById(string tableName, ColumnInfo[] columns, object rowId)
		{
			return GetRowByIdViaConnection(_connectionRegister, tableName, columns, rowId);
		}

		private bool DeleteRowByIdViaConnection(SQLiteConnection connection, string tableName, object rowId)
		{
			try
			{
				int rowsAffected = connection.Execute($"DELETE FROM {tableName} WHERE id = @Id", new { Id = rowId });

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

		public bool DeleteRowById(string tableName, object rowId)
		{
			return DeleteRowByIdViaConnection(_connectionRegister, tableName, rowId);
		}

		private bool DeleteTableViaConnection(SQLiteConnection connection, string tableName)
		{
			try
			{
				connection.Execute($"DROP TABLE {tableName}");

				LogDbActionWithStatus($"{GetLocalizedString("db_del_table_suc")} {tableName} {GetLocalizedString("from")} {connection.DataSource}",
					$"Successfully deleted table {tableName} from {connection.DataSource}.");
				

				return true;
			}
			catch (Exception e)
			{
				LogDbActionWithStatus($"{GetLocalizedString("db_del_table_fail")} {tableName} {GetLocalizedString("from")} {connection.DataSource}",
					$"Failed to delete table {tableName} from {connection.DataSource}.{LineSeparator}" +
					$"Error message: {e.Message}", ERROR);
			}

			return false;
		}

		public bool DeleteTable(string tableName)
		{
			return DeleteTableViaConnection(_connectionRegister, tableName);
		}

		public bool TryParse(CSMColumnType columnType, object data)
		{
			switch (columnType)
			{
				case CSMColumnType.Text:
					return true;
				case CSMColumnType.Integer:
					return long.TryParse(data.ToString(), out long res);
				case CSMColumnType.NullType:
					return false;
			}

			return false;
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
						$"Failed to create table: {dbFilePath}/{tableName}.{LineSeparator}" + 
						$"Error msg: {e.Message}{(debugLogging ? $"{LineSeparator} SQL: {createCommand}" : "")}",
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
