﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ColdStorageManager.Models
{
	public class MySQLDbConnectionProfile : IEquatable<MySQLDbConnectionProfile>
	{
		private const char AttributeSeparator = ';', ValueSeparator = '=';
		private const string EmptyPwValue = "null";
		public string ProfileName { get; set; }
		public string Host { get; set; }
		public ushort Port { get; set; }
		public string DatabaseName { get; set; }
		public string UID { get; set; }
		public string Password { get; set; }

		// source: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type
		
		public override bool Equals(object obj) => this.Equals(obj as MySQLDbConnectionProfile);

		public bool Equals(MySQLDbConnectionProfile profile)
		{
			if (profile is null)
			{
				return false;
			}

			// Optimization for a common success case.
			if (Object.ReferenceEquals(this, profile))
			{
				return true;
			}

			// If run-time types are not exactly the same, return false.
			if (this.GetType() != profile.GetType())
			{
				return false;
			}

			// Return true if the fields match.
			// Note that the base class is not invoked because it is
			// System.Object, which defines Equals as reference equality.
			return (ProfileName.Equals(profile.ProfileName)) && (Host.Equals(profile.Host) &&
																 Port == profile.Port &&
			                                                     DatabaseName.Equals(profile.DatabaseName) &&
			                                                     UID.Equals(profile.UID) &&
			                                                     Password.Equals(profile.Password));
		}

        public MySQLDbConnectionProfile(string profileName, string host, ushort port, string databaseName, string uid, string password)
		{
			ProfileName = profileName;
			Host = host;
			Port = port;
			DatabaseName = databaseName;
			UID = uid;
			Password = password;
		}

        public MySQLDbConnectionProfile(string profileName, string serializedForm)
        {
	        ProfileName = profileName;

	        string[] attributes = serializedForm.Split(AttributeSeparator);
	        Host = attributes[0].Split(ValueSeparator)[1];
	        Port = UInt16.Parse(attributes[1].Split(ValueSeparator)[1]);
	        DatabaseName = attributes[2].Split(ValueSeparator)[1];
	        UID = attributes[3].Split(ValueSeparator)[1];
	        Password = ((attributes[4].Split(ValueSeparator)[1]).Equals(EmptyPwValue) ? "" : (attributes[4].Split(ValueSeparator)[1]));
        }

		public override string ToString()
        {
	        return nameof(Host) + ValueSeparator + Host + AttributeSeparator +
	               nameof(Port) + ValueSeparator + Port + AttributeSeparator +
	               nameof(DatabaseName) + ValueSeparator + DatabaseName + AttributeSeparator +
	               nameof(UID) + ValueSeparator + UID + AttributeSeparator +
	               nameof(Password) + ValueSeparator + (Password.Equals("") ? EmptyPwValue : Password);
        } 
	}
}
