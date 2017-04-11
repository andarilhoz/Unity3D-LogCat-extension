using System;
using UnityEngine;
using System.Collections.Generic;

namespace LogCatExtension
{
	internal class LogCatFilter
	{
		public static List<LogCatLog> FilterLogList( FilterConfiguration config, List<LogCatLog> fullLogList )
		{
			List<LogCatLog> filterLogList = new List<LogCatLog> ();
			// Filter
			foreach (var logCatLog in fullLogList) {
				bool shouldFilterByString = config.filterByString.Length > 1;
				if (!shouldFilterByString || FilterByString (logCatLog, config.filterByString)) {
					
				} else {
					continue;
				}
				if (!config.prefilterOnlyUnity || FilterByUnityString (logCatLog)) {

				} else {
					continue;
				}
				bool filtered = false;
				if (filtered || (config.filterError && FilterByType (logCatLog, 'E'))) {
					filtered = true;
				} 
				if (filtered || (config.filterWarning && FilterByType (logCatLog, 'W'))) {
					filtered = true;
				} 
				if (filtered || (config.filterDebug && FilterByType (logCatLog, 'D'))) {
					filtered = true;
				} 
				if (filtered || (config.filterInfo && FilterByType (logCatLog, 'I'))) {
					filtered = true;
				} 
				if (filtered || (config.filterVerbose && FilterByType (logCatLog, 'V'))) {
					filtered = true;
				} 

				if(filtered)
					filterLogList.Add (logCatLog);
			}
					
			return filterLogList;
		}

		private static bool FilterByString(LogCatLog log, string filterBy){
			return log.Message.ToLower ().Contains (filterBy.ToLower ());
		}

		private static bool FilterByUnityString(LogCatLog log){
			return log.Message.Contains ("Unity");
		}

		private static bool FilterByType(LogCatLog log, char type){
			return log.Type == type;
		}
	}
}