using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Collections.Generic;
using System;

namespace LogCatExtension
{
	public class LogCatWindow : EditorWindow
	{   
	    //COLORS Cache
	    private static readonly Color color_error = new Color( 0.75f, 0.5f, 0.5f, 1f );
	    private static readonly Color color_info = new Color( 0.5f, 0.75f, 0.5f, 1f );
	    private static readonly Color color_warning = new Color( 0.95f, 0.95f, 0.3f, 1f );
	    private static readonly Color color_debug = new Color( 0.5f, 0.5f, 0.75f, 1f );
	    private static readonly Color color_background = new Color( 1.0f, 1.0f, 1.0f, 0.1f );

	    private Texture2D backgroundTexture = null;

		private static FilterConfiguration ViewFilterConfiguration = new FilterConfiguration();
		private static LogCatAdapter LogCatAdapter = new LogCatAdapter();
		
	    // Filtered GUI list scroll position
	    private Vector2 scrollPosition = new Vector2( 0, 0 );

	    // Add menu item named "LogCat" to the Window menu
	    [MenuItem( "Window/LogCat - Android Logger" )]
	    public static void ShowWindow()
	    {
	        // Show existing window instance. If one doesn't exist, make one.
	        GetWindow( typeof( LogCatWindow ), false, "Logcat" );
	    }

	    void OnEnable()
	    {
	        if( EditorPrefs.GetBool( "LogCatWindowEnabled", true ) )
				LogCatAdapter.StartLogCat();

			EditorApplication.update += UpdateView;
	    }

	    void OnDisable()
	    {
			EditorPrefs.SetBool( "LogCatWindowEnabled", LogCatAdapter.IsLogCatProcessRunning() );

			LogCatAdapter.StopLogCat();

			EditorApplication.update -= UpdateView;
	    }

		void UpdateView()
		{
			if(LogCatAdapter.ShouldRepaint())
				Repaint();
		}

	    void OnGUI()
	    {
	        GUILayout.BeginHorizontal();

	        GUI.enabled = true;

			ViewFilterConfiguration.prefilterOnlyUnity = GUILayout.Toggle (ViewFilterConfiguration.prefilterOnlyUnity, "Unity Logs Only", "Button", GUILayout.Width (110f));

	        GUI.color = Color.white;
	        
			if( LogCatAdapter.IsLogCatProcessRunning() && GUILayout.Button( "Stop", GUILayout.Width( 55f ) ) )
	        {
				LogCatAdapter.StopLogCat();
	        }
			else if( !LogCatAdapter.IsLogCatProcessRunning() && GUILayout.Button( "Start", GUILayout.Width( 55f ) ) )
	        {
				LogCatAdapter.StartLogCat();
	        }
	        if( GUILayout.Button( "Clear", GUILayout.Width( 55f ) ) )
	        {
				LogCatAdapter.ClearLogCat();
	        }

	        // Create filters
			ViewFilterConfiguration.filterByString = GUILayout.TextField( ViewFilterConfiguration.filterByString, GUILayout.Height( 20f ) );
	        GUI.color = color_error;
			ViewFilterConfiguration.filterError = GUILayout.Toggle( ViewFilterConfiguration.filterError, "Error", "Button", GUILayout.Width( 60f ) );
	        GUI.color = color_warning;
			ViewFilterConfiguration.filterWarning = GUILayout.Toggle( ViewFilterConfiguration.filterWarning, "Warning", "Button", GUILayout.Width( 60f ) );
	        GUI.color = color_debug;
			ViewFilterConfiguration.filterDebug = GUILayout.Toggle( ViewFilterConfiguration.filterDebug, "Debug", "Button", GUILayout.Width( 60f ) );
	        GUI.color = color_info;
			ViewFilterConfiguration.filterInfo = GUILayout.Toggle( ViewFilterConfiguration.filterInfo, "Info", "Button", GUILayout.Width( 60f ) );
	        GUI.color = Color.white;
			ViewFilterConfiguration.filterVerbose = GUILayout.Toggle( ViewFilterConfiguration.filterVerbose, "Verbose", "Button", GUILayout.Width( 60f ) );

	        GUILayout.EndHorizontal();

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
