using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace LogCatExtension
{
	internal class LogCatFilter
	{
		private FilterConfiguration testConfig;
		private LogCatLog testLog;
		private List<LogCatLog> testLogList;

		[TestFixtureSetUp]
		public void Init() {
			testConfig = new FilterConfiguration();
			testLog = new LogCatLog ("E/TestLogCat: level:86, scale:100, status:3, health:2, present:true ");
			testLogList = new List<LogCatLog> ();
		}

		[TestFixtureTearDown]
		public void CleanUp() {
			testConfig = null;
			testLog = null;
			testLogList.Clear ();
		}

		[SetUp]
		public void SetUp() {
			testConfig.prefilterOnlyUnity = false;
			testConfig.filterError = true;
			testConfig.filterWarning = true;
			testConfig.filterDebug = true;
			testConfig.filterInfo = true;
			testConfig.filterVerbose = true;
			testConfig.filterByString = "scale";
			testConfig.filterByRegex = "[$~:^]";
			testLogList.Add (testLog);
		}

		[Test]
		public void LogCatFilterTest()
		{
			Assert.IsTrue(FilterByType(testLog, 'E'));
		}

		[Test]
		public void LogCatFilterStringTest()
		{
			Assert.IsTrue(FilterByString(testLog, testConfig.filterByString));
		}
			
		[Test]
		public void LogCatFilterRegexTest()
		{
			Assert.IsFalse(FilterByRegex(testLog, "[$~^]"));
			Assert.IsTrue(FilterByRegex(testLog, testConfig.filterByRegex));
		}

		[Test]
		public void LogCatFilterFlowTest()
		{
			Assert.IsTrue((FilterLogList(testConfig, testLogList).Count == 1));
		}

		[TearDown]
		public void TearDown() {
			//log?
		}

		public static List<LogCatLog> FilterLogList( FilterConfiguration config, List<LogCatLog> fullLogList )
		{
			List<LogCatLog> filterLogList = new List<LogCatLog> ();
			// Filter
			foreach (var logCatLog in fullLogList) {
				if (FilterLogCatLog (logCatLog, config)) {
					filterLogList.Add (logCatLog);
				}
			}
					
			return filterLogList;
		}

		private static bool FilterLogCatLog(LogCatLog logCatLog, FilterConfiguration config){
			bool shouldFilterByString = config.filterByString.Length > 1;
			if (!shouldFilterByString || FilterByString (logCatLog, config.filterByString)) {
				
			} else {
				return false;
			}
			bool shouldFilterByRegex = config.filterByRegex.Length > 1;
			if (!shouldFilterByRegex || FilterByRegex (logCatLog, config.filterByRegex)) {

			} else {
				return false;
			}
			if (!config.prefilterOnlyUnity || FilterByUnityString (logCatLog)) {

			} else {
				return false;
			}
			if (!config.filterTime || FilterByTimeSpan (logCatLog, config.filterByTimeFrom, config.filterByTimeTo)) {

			} else {
				return false;
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

			return filtered;
		}

		private static bool FilterByString(LogCatLog log, string filterBy){
			return log.Message.ToLower ().Contains (filterBy.ToLower ());
		}

		private static bool FilterByRegex(LogCatLog log, string filterBy){
			if (string.IsNullOrEmpty (filterBy)) {
				return false;
			}

			bool filtered = false;

			try
			{
				Regex rgx = new Regex(filterBy);
				filtered = rgx.IsMatch(log.Message);
			}
			catch (ArgumentException)
			{
				return false;
			}

			return filtered;
		}

		private static bool FilterByUnityString(LogCatLog log){
			return log.Message.Contains ("Unity");
		}

		private static bool FilterByType(LogCatLog log, char type){
			return log.Type == type;
		}

		private static bool FilterByTimeSpan(LogCatLog log, string filterTimeFrom, string filterTimeTo){
			TimeSpan logTimeTamp = log.TimeSpamp.TimeOfDay;
			TimeSpan timeFrom = TimeSpan.Parse(filterTimeFrom);
			TimeSpan timeTo = TimeSpan.Parse(filterTimeTo);

			if ((logTimeTamp > timeFrom) && (logTimeTamp < timeTo))
			{
				return true;
			}

			return false;
		}
	}
}