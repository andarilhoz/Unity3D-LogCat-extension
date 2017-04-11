using System;
using UnityEngine;

namespace LogCatExtension
{
	internal class FilterConfiguration
	{
		// Filters
		public bool prefilterOnlyUnity = true;
		public bool filterError = true;
		public bool filterWarning = true;
		public bool filterDebug = true;
		public bool filterInfo = true;
		public bool filterVerbose = true;
		public string filterByString = String.Empty;
	}
}