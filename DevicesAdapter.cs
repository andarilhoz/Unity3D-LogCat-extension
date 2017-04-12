using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Collections.Generic;
using System;

namespace LogCatExtension
{
	internal class DevicesAdapter
	{
		//LOGCAT output format
		private const string DEVICES = "devices -l";

		// Android adb devices process
		private Process devicesProcess = null;

		//Devices list
		private List<Device> devicesList = new List<Device> ();

		public DevicesAdapter()
		{
		}

		ProcessStartInfo DefaultProcestStartInfo(){
			ProcessStartInfo logProcessInfo = new ProcessStartInfo();
			logProcessInfo.CreateNoWindow = false;
			logProcessInfo.UseShellExecute = false;
			logProcessInfo.RedirectStandardOutput = true;
			logProcessInfo.RedirectStandardError = true;
			logProcessInfo.FileName = EditorPrefs.GetString( "AndroidSdkRoot" ) + "/platform-tools/adb";
			logProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
			return logProcessInfo;
		}

		public void RefreshDevices()
		{
			if( devicesProcess != null )
				StopDevices();

			// Start `adb logcat` (with additional optional arguments) process for filtering
			ProcessStartInfo logProcessInfo = DefaultProcestStartInfo ();
			logProcessInfo.Arguments = DEVICES;
			devicesProcess = Process.Start( logProcessInfo );
			devicesProcess.OutputDataReceived += ( sender, outputLine ) =>
			{
				if( outputLine.Data != null && outputLine.Data.Length > 2 ){
					if(outputLine.Data.Substring(0, outputLine.Data.IndexOf(" ")) == "List"){
						devicesList.Clear ();
					}
					else{
						devicesList.Add( new Device( outputLine.Data ) );
					}
				}
			};
			devicesProcess.BeginOutputReadLine();
		}
			

		public void StopDevices()
		{
			if( devicesProcess == null )
				return;

			try
			{
				devicesProcess.Kill();
			}
			#pragma warning disable 0168
			catch( InvalidOperationException ex )
			#pragma warning restore 0168
			{
				// Just ignore it.
			}
			finally
			{
				devicesProcess = null;
			}
		}

		public List<Device> GetDevicesList(){
			return devicesList;
		}
	}
}