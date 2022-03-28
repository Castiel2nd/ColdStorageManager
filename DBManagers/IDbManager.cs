using System;
using System.Collections.Generic;
using System.Text;
using ColdStorageManager.Models;

namespace ColdStorageManager.DBManagers
{
	internal interface IDbManager
	{
		public List<Capture> GetCaptures();

		public void SaveCapture(Capture capture);

		// id is contained in Capture instance itself
		public void UpdateCaptureById(Capture capture);

		public void DeleteCapture(Capture capture);

	}
}
