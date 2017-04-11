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
	    
	    // Filters
	    private bool prefilterOnlyUnity = true;
	    private bool filterError = true;
	    private bool filterWarning = true;
	    private bool filterDebug = true;
	    private bool filterInfo = true;
	    private bool filterVerbose = true;
	    private string filterByString = String.Empty;

	    private Texture2D backgroundTexture = null;

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

			EditorApplication.update += LogCatAdapter.UpdateLogs;
	    }

	    void OnDisable()
	    {
			EditorPrefs.SetBool( "LogCatWindowEnabled", LogCatAdapter.logCatProcess != null );

			LogCatAdapter.StopLogCat();

			EditorApplication.update -= LogCatAdapter.UpdateLogs;
	    }


	    void OnGUI()
	    {
	        GUILayout.BeginHorizontal();

	        GUI.enabled = true;
			if(prefilterOnlyUnity != GUILayout.Toggle( prefilterOnlyUnity, "Unity Logs Only", "Button", GUILayout.Width( 110f ) ))
			{
				prefilterOnlyUnity = !prefilterOnlyUnity;
				if (LogCatAdapter.logCatProcess != null) {
					LogCatAdapter.StartLogCat ();
				}
			}

	        GUI.color = Color.white;
	        
			if( LogCatAdapter.logCatProcess != null && GUILayout.Button( "Stop", GUILayout.Width( 55f ) ) )
	        {
				LogCatAdapter.StopLogCat();
	        }
			else if( LogCatAdapter.logCatProcess == null && GUILayout.Button( "Start", GUILayout.Width( 55f ) ) )
	        {
				LogCatAdapter.StartLogCat();
	        }

	        if( GUILayout.Button( "Clear", GUILayout.Width( 55f ) ) )
	        {
				LogCatAdapter.ClearLogCat();
	        }

	        // Create filters
	        filterByString = GUILayout.TextField( filterByString, GUILayout.Height( 20f ) );
	        GUI.color = color_error;
	        filterError = GUILayout.Toggle( filterError, "Error", "Button", GUILayout.Width( 60f ) );
	        GUI.color = color_warning;
	        filterWarning = GUILayout.Toggle( filterWarning, "Warning", "Button", GUILayout.Width( 60f ) );
	        GUI.color = color_debug;
	        filterDebug = GUILayout.Toggle( filterDebug, "Debug", "Button", GUILayout.Width( 60f ) );
	        GUI.color = color_info;
	        filterInfo = GUILayout.Toggle( filterInfo, "Info", "Button", GUILayout.Width( 60f ) );
	        GUI.color = Color.white;
	        filterVerbose = GUILayout.Toggle( filterVerbose, "Verbose", "Button", GUILayout.Width( 60f ) );

	        GUILayout.EndHorizontal();

	        GUIStyle lineStyle = new GUIStyle();
	        lineStyle.normal.background = MakeTexture( 600, 1, color_background );

	        scrollPosition = GUILayout.BeginScrollView( scrollPosition, GUILayout.Height( Screen.height - 45 ) );

	        // Show log entries
	        bool shouldFilterByString = filterByString.Length > 1;
			for( int index = LogCatAdapter.oldestLogIndex, i = 0; i < count; i++ )
	        {
				LogCatLog log = LogCatAdapter.logsList[index];

	            // Filter
	            if( ( !shouldFilterByString || log.Message.ToLower().Contains( filterByString.ToLower() ) ) &&
	                (  filterError && log.Type == 'E'
	                || filterWarning && log.Type == 'W'
	                || filterDebug && log.Type == 'D'
	                || filterInfo && log.Type == 'I'
	                || filterVerbose && log.Type == 'V' ) )
	            {
	                GUI.backgroundColor = log.GetBgColor();
					EditorGUILayout.BeginHorizontal( lineStyle );
					{
						EditorGUILayout.SelectableLabel(log.Message, EditorStyles.label, GUILayout.Height(EditorGUIUtility.singleLineHeight));
					}
					EditorGUILayout.EndHorizontal();
	            }

				index = ( index + 1 ) % LogCatAdapter.CAPACITY;
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
