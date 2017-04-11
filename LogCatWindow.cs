using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using System.Globalization;

namespace LogCatExtension
{
	public class LogCatWindow : EditorWindow
	{   
		//LogCatExtensions 
		private static FilterConfiguration ViewFilterConfiguration = new FilterConfiguration();
		private static LogCatAdapter LogCatAdapter = new LogCatAdapter();
		private static DevicesAdapter DevicesAdapter = new DevicesAdapter();

	    //COLORS Cache
	    private static readonly Color color_error = new Color( 0.75f, 0.5f, 0.5f, 1f );
	    private static readonly Color color_info = new Color( 0.5f, 0.75f, 0.5f, 1f );
	    private static readonly Color color_warning = new Color( 0.95f, 0.95f, 0.3f, 1f );
	    private static readonly Color color_debug = new Color( 0.5f, 0.5f, 0.75f, 1f );
	    private static readonly Color color_background = new Color( 1.0f, 1.0f, 1.0f, 0.1f );

	    private Texture2D backgroundTexture = null;

	    // Filtered GUI list scroll position
	    private Vector2 scrollPosition = new Vector2( 0, 0 );

		// Toggles
		int selectedToggle = 0;
		string[] toggleOptions = new string[] { "Str", "Regex", "TimeSpan" };

		// Devices
		int selectedDevice = 0;
		string selectedDeviceId = String.Empty;

		// Time needed Info
		private CultureInfo culture = CultureInfo.CreateSpecificCulture("en-AU");

	    // Add menu item named "LogCat" to the Window menu
	    [MenuItem( "Window/LogCat - Android Logger" )]
	    public static void ShowWindow()
	    {
	        // Show existing window instance. If one doesn't exist, make one.
	        GetWindow( typeof( LogCatWindow ), false, "Logcat" );
	    }

	    void OnEnable()
	    {
			DevicesAdapter.RefreshDevices ();
			RefreshFirstDevice ();

	        if( EditorPrefs.GetBool( "LogCatWindowEnabled", true ) )
				LogCatAdapter.StartLogCat(selectedDeviceId);

			EditorApplication.update += UpdateView;
	    }

	    void OnDisable()
	    {
			EditorPrefs.SetBool( "LogCatWindowEnabled", LogCatAdapter.IsLogCatProcessRunning() );

			LogCatAdapter.StopLogCat();
			DevicesAdapter.StopDevices ();
			EditorApplication.update -= UpdateView;
	    }

		void UpdateView()
		{
			if(LogCatAdapter.ShouldRepaint())
				Repaint();
		}

	    void OnGUI()
	    {
			onGuiTopView ();
			onGuiLogCatView ();
	    }

		private void onGuiTopView(){
			GUILayout.BeginHorizontal();
			GUI.enabled = true;
			GUI.color = Color.white;

			onGiuDevicesDropDown ();

			onGuiUnityOnlyButton ();

			onGuiStartStopButtons ();	

			onGuiFilterTabs ();

			onGuiFilterButtons ();

			GUILayout.EndHorizontal();
		}

		private void onGiuDevicesDropDown ()
		{
			GUILayout.Label ("Devices", GUILayout.Width (50f));
			var listOfDevices = DevicesAdapter.GetDevicesList ();
			if (listOfDevices.Count > 0) {

				if (selectedDevice >= listOfDevices.Count) {
					selectedDeviceId = String.Empty;
					selectedDevice = 0;
				}

				string[] devicesNameStrings = new string[listOfDevices.Count];
				string[] devicesIdStrings = new string[listOfDevices.Count];

				int i = 0;
				foreach (var device in listOfDevices) {
					devicesNameStrings [i] = device.Name;
					devicesIdStrings [i] = device.ID;
					i++;
				}

				int oldSelectedDevice = selectedDevice;
				selectedDevice = EditorGUILayout.Popup (selectedDevice, devicesNameStrings, GUILayout.Width (110f));
				selectedDeviceId = devicesIdStrings [selectedDevice];
				if (oldSelectedDevice != selectedDevice) {
					if (LogCatAdapter.IsLogCatProcessRunning ()) {
						LogCatAdapter.StartLogCat (selectedDeviceId);
					}
				}
			} else {
				selectedDeviceId = String.Empty;
				selectedDevice = 0;
			}
		}

		private void RefreshFirstDevice ()
		{
			var listOfDevices = DevicesAdapter.GetDevicesList ();
			if (listOfDevices.Count > 0) {
				string[] devicesNameStrings = new string[listOfDevices.Count];
				string[] devicesIdStrings = new string[listOfDevices.Count];

				int i = 0;
				foreach (var device in listOfDevices) {
					devicesNameStrings [i] = device.Name;
					devicesIdStrings [i] = device.ID;
					i++;
				}

				selectedDeviceId = devicesIdStrings [selectedDevice];
			} 
		}

		private void onGuiUnityOnlyButton(){
			ViewFilterConfiguration.prefilterOnlyUnity = GUILayout.Toggle (ViewFilterConfiguration.prefilterOnlyUnity, "Unity Logs Only", "Button", GUILayout.Width (110f));
		}

		private void onGuiStartStopButtons(){
			if( LogCatAdapter.IsLogCatProcessRunning() && GUILayout.Button( "Stop", GUILayout.Width( 55f ) ) )
			{
				LogCatAdapter.StopLogCat();
				DevicesAdapter.RefreshDevices ();
			}
			else if( !LogCatAdapter.IsLogCatProcessRunning() && GUILayout.Button( "Start", GUILayout.Width( 55f ) ) )
			{
				LogCatAdapter.StartLogCat(selectedDeviceId);
				DevicesAdapter.RefreshDevices ();
			}
			if( GUILayout.Button( "Clear", GUILayout.Width( 55f ) ) )
			{
				LogCatAdapter.ClearLogCat();
				DevicesAdapter.RefreshDevices ();
			}
		}

		private void onGuiFilterTabs(){
			selectedToggle = GUILayout.SelectionGrid(selectedToggle, toggleOptions, toggleOptions.Length, EditorStyles.miniButton, GUILayout.Width( 200f ));
			if (selectedToggle == 0)
			{
				ViewFilterConfiguration.filterByString = GUILayout.TextField (ViewFilterConfiguration.filterByString, GUILayout.Height (20f));
				ViewFilterConfiguration.filterByRegex = string.Empty;
				ViewFilterConfiguration.filterTime = false;
				GUI.color = color_error;
			}
			if (selectedToggle == 1)
			{
				ViewFilterConfiguration.filterByRegex = GUILayout.TextField (ViewFilterConfiguration.filterByRegex, GUILayout.Height (20f));
				ViewFilterConfiguration.filterByString = string.Empty;
				ViewFilterConfiguration.filterTime = false;
				GUI.color = color_error;
			}
			if (selectedToggle == 2)
			{
				ViewFilterConfiguration.filterByTimeFrom = GUILayout.TextField (ViewFilterConfiguration.filterByTimeFrom, GUILayout.Height (20f));
				ViewFilterConfiguration.filterByTimeTo = GUILayout.TextField (ViewFilterConfiguration.filterByTimeTo, GUILayout.Height (20f));

				DateTime timeTester;
				bool textFromTimeValid = DateTime.TryParse(ViewFilterConfiguration.filterByTimeFrom, culture, DateTimeStyles.None, out timeTester);
				bool textToTimeValid = DateTime.TryParse(ViewFilterConfiguration.filterByTimeTo, culture, DateTimeStyles.None, out timeTester);

				ViewFilterConfiguration.filterTime = (textFromTimeValid && textToTimeValid);
			}
		}

		private void onGuiFilterButtons(){
			ViewFilterConfiguration.filterError = GUILayout.Toggle( ViewFilterConfiguration.filterError, "Error", "Button", GUILayout.Width( 60f ) );
			GUI.color = color_warning;
			ViewFilterConfiguration.filterWarning = GUILayout.Toggle( ViewFilterConfiguration.filterWarning, "Warning", "Button", GUILayout.Width( 60f ) );
			GUI.color = color_debug;
			ViewFilterConfiguration.filterDebug = GUILayout.Toggle( ViewFilterConfiguration.filterDebug, "Debug", "Button", GUILayout.Width( 60f ) );
			GUI.color = color_info;
			ViewFilterConfiguration.filterInfo = GUILayout.Toggle( ViewFilterConfiguration.filterInfo, "Info", "Button", GUILayout.Width( 60f ) );
			GUI.color = Color.white;
			ViewFilterConfiguration.filterVerbose = GUILayout.Toggle( ViewFilterConfiguration.filterVerbose, "Verbose", "Button", GUILayout.Width( 60f ) );
		}

		private void onGuiLogCatView(){
			GUIStyle lineStyle = new GUIStyle();
			lineStyle.normal.background = MakeTexture( 600, 1, color_background );

			scrollPosition = GUILayout.BeginScrollView( scrollPosition, GUILayout.Height( Screen.height - 45 ) );

			List<LogCatLog> fullLogCatList = LogCatAdapter.GetLogsList ();
			List<LogCatLog> filteredLogCatList = LogCatFilter.FilterLogList (ViewFilterConfiguration, fullLogCatList);

			// Show log entries
			foreach (var logCatLog in filteredLogCatList) {
				GUI.backgroundColor = logCatLog.GetBgColor();
				EditorGUILayout.BeginHorizontal( lineStyle );
				{
					EditorGUILayout.SelectableLabel(logCatLog.Message, EditorStyles.label, GUILayout.Height(EditorGUIUtility.singleLineHeight));
				}
				EditorGUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();
		}

	    private Texture2D MakeTexture( int width, int height, Color col )
	    {
	        if( backgroundTexture == null )
	        {
	            Color[] pix = new Color[width * height];

	            for( int i = 0; i < pix.Length; i++ )
	                pix[i] = col;

	            backgroundTexture = new Texture2D( width, height );
	            backgroundTexture.SetPixels( pix );
	            backgroundTexture.Apply();
	        }

	        return backgroundTexture;
	    }
	}
}
