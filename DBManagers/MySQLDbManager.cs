using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using ColdStorageManager.Models;
using MySql.Data.MySqlClient;
using static ColdStorageManager.Globals;

namespace ColdStorageManager.DBManagers
{
	internal class MySQLDbManager : IDbManager
	{
		private MySQLDbConnectionProfile _connectionProfile;
		private MySqlConnection _connection;
		private string _connectionString = "server={0};port={1};database={2};uid={3};pwd={4};";

		private Window _msgBoxOwner;

		public MySQLDbManager(MySQLDbConnectionProfile connectionProfile, bool isTest, Window msgBoxOwner = null)
		{
			_connectionProfile = connectionProfile;
			_msgBoxOwner = msgBoxOwner;

			if (CreateConnection())
			{
				if (isTest)
				{
					_connection.Close();
					return;
				}
				else
				{

				}
			}
		}

		~MySQLDbManager()
		{
			_connection.Close();
		}

		private bool CreateConnection()
		{
			_connectionString = string.Format(_connectionString, _connectionProfile.Host, _connectionProfile.Port,
				_connectionProfile.DatabaseName, _connectionProfile.UID, _connectionProfile.Password);

			try
			{
				_connection = new MySqlConnection(_connectionString);
				_connection.Open();

				DisplayInfoMessageBoxWithOwner(GetLocalizedString("success_connection_title"),
					GetLocalizedString("success_connection") + _connectionProfile.Host + ":" + _connectionProfile.Port,
							(_msgBoxOwner == null ? mainWindow : _msgBoxOwner));
			}
			catch (MySqlException e)
			{
				DisplayErrorMessageBoxWithDescriptionWithOwner(GetLocalizedString("error_connection_title"),
					GetLocalizedString("error_connection") + _connectionProfile.Host + ":" + _connectionProfile.Port + ".",
					e.Message,
					(_msgBoxOwner == null ? mainWindow : _msgBoxOwner));
				return false;
			}
			catch(AggregateException e)
			{
				DisplayErrorMessageBoxWithDescriptionWithOwner(GetLocalizedString("error_connection_title"),
					GetLocalizedString("error_connection") + _connectionProfile.Host + ":" + _connectionProfile.Port + ".",
					e.Message,
					(_msgBoxOwner == null ? mainWindow : _msgBoxOwner));
				return false;
			}

			return true;
		}

		public List<Capture> GetCaptures()
		{
			throw new NotImplementedException();
		}

		public void SaveCapture(Capture capture)
		{
			throw new NotImplementedException();
		}

		public void UpdateCaptureById(Capture capture)
		{
			throw new NotImplementedException();
		}

		public void DeleteCapture(Capture capture)
		{
			throw new NotImplementedException();
		}
	}
}
