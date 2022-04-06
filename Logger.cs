using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using ColdStorageManager.Annotations;
using static ColdStorageManager.Globals;

namespace ColdStorageManager
{
	public class LogHolder : INotifyPropertyChanged
	{
		public string log => logStrBuilder.ToString();

		public StringBuilder logStrBuilder = new StringBuilder();

		public string databaseLog => databaseLogStrBuilder.ToString();
		public StringBuilder databaseLogStrBuilder = new StringBuilder();

		private string _status;
		public string Status
		{
			get => _status;
			set
			{
				_status = value;
				OnPropertyChanged(nameof(Status));
			}
		}

		private string _dbStatus;
		public string DbStatus
		{
			get => _dbStatus;
			set
			{
				_dbStatus = value;
				OnPropertyChanged(nameof(DbStatus));
			}
		}

		public void AppendDbLog(string line)
		{
			databaseLogStrBuilder.Append($"[{DateTime.Now}]: {line}{LineSeparator}");
			OnPropertyChanged(nameof(databaseLog));
		}

		public void AppendLog(string line)
		{
			logStrBuilder.Append($"[{DateTime.Now}]: {line}{LineSeparator}");
			OnPropertyChanged(nameof(log));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}


	public static class Logger
	{
		public const string NORMAL = "NORMAL";
		public const string WARNING = "WARNING";
		public const string ERROR = "ERROR";

		//logging
		public static void LogAction(string statusMsg, string logMsg, string type = NORMAL)
		{
			logHolder.Status = statusMsg;
			logHolder.AppendLog($"{type}	|	{logMsg}");
		}

		public static void LogActionSameMsg(string msg, string type = NORMAL)
		{
			LogAction(msg, msg, type);
		}

		//db logging
		public static TextBlock statusBarTb;
		public static TextBlock dbStatusBarTb;
		public static Ellipse dbStatusEllipse;

		public static void LogDbAction(string statusMsg, string logMsg, string type = NORMAL)
		{
			logHolder.DbStatus = statusMsg;
			logHolder.AppendDbLog($"{type}	|	{logMsg}");
		}

		public static void LogDbActionSameMsg(string msg, string type = NORMAL)
		{
			LogDbAction(msg, msg, type);
		}

		public static void LogDbActionWithMsgBox(string displayMsg, string logMsg, string windowTitle,
			MessageBoxImage type, string exceptionMsg = default)
		{
			LogDbActionWithMsgBoxWithOwner(displayMsg, logMsg, windowTitle, type, mainWindow, exceptionMsg);
		}

		public static void LogDbActionWithMsgBox(string msg, string windowTitle, MessageBoxImage type,
			string exceptionMsg = default)
		{
			LogDbActionWithMsgBox(msg, msg, windowTitle, type, exceptionMsg);
		}

		public static void LogDbActionWithMsgBoxWithOwner(string displayMsg,
			string logMsg, string windowTitle,
			MessageBoxImage type, Window owner = default, string exceptionMsg = default)
		{
			string log;
			switch (type)
			{
				case MessageBoxImage.Error:
					DisplayErrorMessageBoxWithDescriptionWithOwner(windowTitle, displayMsg, exceptionMsg, owner);
					log = $"{ERROR}	|	{logMsg} {exceptionMsg}";
					break;
				case MessageBoxImage.Information:
					DisplayInfoMessageBoxWithOwner(windowTitle, displayMsg, owner);
					log = $"{NORMAL}	|	{logMsg}";
					break;
				default:
					log = $"{NORMAL}	|	{logMsg}";
					break;
			}

			logHolder.DbStatus = logMsg;
			logHolder.AppendDbLog(log);
		}

		public static void LogDbActionWithMsgBoxWithOwner(string msg, string windowTitle,
			MessageBoxImage type, Window owner, string exceptionMsg = default)
		{
			LogDbActionWithMsgBoxWithOwner(msg, msg, windowTitle, type, owner, exceptionMsg);
		}
	}
}
