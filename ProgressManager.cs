using System;
using System.Collections.Generic;
using System.Text;
using Org.BouncyCastle.Asn1.X509.Qualified;

namespace ColdStorageManager
{
	public static class ProgressManager
	{
		public static IProgress<T> GetUpdateFrontendPropertyProgress<T>(object obj, string propertyName)
		{
			IProgress<T> progress = new Progress<T> (obj1 => obj.GetType().GetProperty(propertyName).SetValue(obj, obj1));
			
			return progress;
		}
	}
}
