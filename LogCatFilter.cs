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
				if( (!shouldFilterByString || logCatLog.Message.ToLower().Contains( config.filterByString.ToLower() ) ) &&
					(!config.prefilterOnlyUnity || logCatLog.Message.Contains("Unity") )
					&&
					(  config.filterError && logCatLog.Type == 'E'
						|| config.filterWarning && logCatLog.Type == 'W'
						|| config.filterDebug && logCatLog.Type == 'D'
						|| config.filterInfo && logCatLog.Type == 'I'
						|| config.filterVerbose && logCatLog.Type == 'V' ) )
				{
					filterLogList.Add (logCatLog);
				}
			}
					
			return filterLogList;
		}
	}
}