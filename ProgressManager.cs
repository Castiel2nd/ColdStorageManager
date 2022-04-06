using System;
using System.Collections.Generic;
using System.Text;
using Org.BouncyCastle.Asn1.X509.Qualified;

namespace ColdStorageManager
{
	public static class ProgressManager
	{
		public static void UpdateFrontendProperty<T>(object obj, string propertyName, T value)
		{
			IProgress<T> progress = new Progress<T> (obj1 => obj.GetType().GetProperty(propertyName).SetValue(obj, obj1));
			progress.Report(value);
		}
	}
}
