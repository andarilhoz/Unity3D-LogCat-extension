﻿using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Collections.Generic;
using System;

namespace LogCatExtension
{
	internal class LogCatAdapter
	{
		//LOGCAT output format
		private const string LOGCAT = "logcat -v tag";

		// How many log entries to show in unity3D editor. Keep it low for better performance.
		private const int CAPACITY = 300;

		private const double UPDATE_FREQUENCY = 0.1;

		private double nextUpdateTime = 0f;

		// Android adb logcat process
		private Process logCatProcess = null;

		// Log entries
		private Queue<LogCatLog> waitingLogs = new Queue<LogCatLog>( 10 );
		private LogCatLog[] logsList = new LogCatLog[CAPACITY];

		private int oldestLogIndex = 0;
		private int nextLogIndex = 0;
		private int count = 0;

		public LogCatAdapter()
		{
		}


		private void StartLogCat()
		{
			if( logCatProcess != null )
				StopLogCat();

			// Start `adb logcat` (with additional optional arguments) process for filtering
			ProcessStartInfo logProcessInfo = new ProcessStartInfo();
			logProcessInfo.CreateNoWindow = false;
			logProcessInfo.UseShellExecute = false;
			logProcessInfo.RedirectStandardOutput = true;
			logProcessInfo.RedirectStandardError = true;
			logProcessInfo.FileName = EditorPrefs.GetString( "AndroidSdkRoot" ) + "/platform-tools/adb";
			logProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;

			// Add additional -s argument for filtering by Unity tag.
			logProcessInfo.Arguments = "devices -l";//LOGCAT + ( prefilterOnlyUnity ? " -s Unity" : "" );

			logCatProcess = Process.Start( logProcessInfo );

			logCatProcess.ErrorDataReceived += ( sender, errorLine ) =>
			{
				if( errorLine.Data != null && errorLine.Data.Length > 2 )
					AddLog( new LogCatLog( errorLine.Data ) );
			};
			logCatProcess.OutputDataReceived += ( sender, outputLine ) =>
			{
				if( outputLine.Data != null && outputLine.Data.Length > 2 )
					AddLog( new LogCatLog( outputLine.Data ) );
			};
			logCatProcess.BeginErrorReadLine();
			logCatProcess.BeginOutputReadLine();

			oldestLogIndex = 0;
			nextLogIndex = 0;
			count = 0;
		}

		private void ClearLogCat()
		{
			bool restartLogCat = false;
			if( logCatProcess != null )
			{
				restartLogCat = true;
				StopLogCat();
			}

			// Start `adb logcat` with -c argument
			ProcessStartInfo logClearProcessInfo = new ProcessStartInfo();
			logClearProcessInfo.CreateNoWindow = true;
			logClearProcessInfo.UseShellExecute = false;
			logClearProcessInfo.FileName = EditorPrefs.GetString( "AndroidSdkRoot" ) + "/platform-tools/adb";
			logClearProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;

			// Add additional -s argument for filtering by Unity tag.
			logClearProcessInfo.Arguments = LOGCAT + " -c";

			Process.Start( logClearProcessInfo );

			if( restartLogCat )
				StartLogCat();
			else
			{
				oldestLogIndex = 0;
				nextLogIndex = 0;
				count = 0;
			}
		}

		private void StopLogCat()
		{
			if( logCatProcess == null )
				return;

			try
			{
				logCatProcess.Kill();
			}
			#pragma warning disable 0168
			catch( InvalidOperationException ex )
			#pragma warning restore 0168
			{
				// Just ignore it.
			}
			finally
			{
				logCatProcess = null;
			}
		}


		void UpdateLogs()
		{
			if( EditorApplication.timeSinceStartup > nextUpdateTime )
			{
				bool shouldRepaint = false;

				lock( waitingLogs )
				{
					int waitingCount = waitingLogs.Count;
					if( waitingCount > 0 )
					{
						if( waitingCount > CAPACITY )
						{
							int logsToIgnore = waitingCount - CAPACITY;
							for( int i = 0; i < logsToIgnore; i++ )
							{
								waitingLogs.Dequeue();
							}

							waitingCount = CAPACITY;
						}

						int logsToDiscard = count + waitingCount - CAPACITY;
						if( logsToDiscard > 0 )
						{
							oldestLogIndex = ( oldestLogIndex + logsToDiscard ) % CAPACITY;
							count -= logsToDiscard;
						}

						for( int i = 0; i < waitingCount; i++ )
						{
							logsList[nextLogIndex] = waitingLogs.Dequeue();
							nextLogIndex = ( nextLogIndex + 1 ) % CAPACITY;
						}

						count += waitingCount;

						shouldRepaint = true;
					}
				}

				if( shouldRepaint )
					//TODO: BOOLEAN RETURN EXTRACTION Repaint();

				nextUpdateTime = EditorApplication.timeSinceStartup + UPDATE_FREQUENCY;
			}
		}


		private void AddLog( LogCatLog log )
		{
			lock ( waitingLogs )
			{
				waitingLogs.Enqueue( log );
			}
		}
	}
}