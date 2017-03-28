using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Collections.Generic;
using System;

public class LogCatWindow : EditorWindow
{
    // How many log entries to show in unity3D editor. Keep it low for better performance.
    private const int CAPACITY = 300;

    private const double UPDATE_FREQUENCY = 0.1;

    private double nextUpdateTime = 0f;
    
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

    // Android adb logcat process
    private Process logCatProcess = null;

    // Log entries
    private Queue<LogCatLog> waitingLogs = new Queue<LogCatLog>( 10 );
    private LogCatLog[] logsList = new LogCatLog[CAPACITY];

    private int oldestLogIndex = 0;
    private int nextLogIndex = 0;
    private int count = 0;

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
            StartLogCat();

        EditorApplication.update += UpdateLogs;
    }

    void OnDisable()
    {
        EditorPrefs.SetBool( "LogCatWindowEnabled", logCatProcess != null );

        StopLogCat();

        EditorApplication.update -= UpdateLogs;
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
                Repaint();

            nextUpdateTime = EditorApplication.timeSinceStartup + UPDATE_FREQUENCY;
        }
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();

        GUI.enabled = logCatProcess == null;
        prefilterOnlyUnity = GUILayout.Toggle( prefilterOnlyUnity, "Unity Logs Only", "Button", GUILayout.Width( 110f ) );
        GUI.enabled = true;

        GUI.color = Color.white;
        
        if( logCatProcess != null && GUILayout.Button( "Stop", GUILayout.Width( 55f ) ) )
        {
            StopLogCat();
        }
        else if( logCatProcess == null && GUILayout.Button( "Start", GUILayout.Width( 55f ) ) )
        {
            StartLogCat();
        }

        GUI.enabled = true;
        if( GUILayout.Button( "Clear", GUILayout.Width( 55f ) ) )
        {
            oldestLogIndex = 0;
            nextLogIndex = 0;
            count = 0;
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
        for( int index = oldestLogIndex, i = 0; i < count; i++ )
        {
            LogCatLog log = logsList[index];

            // Filter
            if( ( !shouldFilterByString || log.Message.ToLower().Contains( filterByString.ToLower() ) ) &&
                (  filterError && log.Type == 'E'
                || filterWarning && log.Type == 'W'
                || filterDebug && log.Type == 'D'
                || filterInfo && log.Type == 'I'
                || filterVerbose && log.Type == 'V' ) )
            {
                GUI.backgroundColor = log.GetBgColor();
                GUILayout.BeginHorizontal( lineStyle );
                GUILayout.Label( log.Message );
                GUILayout.EndHorizontal();
            }

            index = ( index + 1 ) % CAPACITY;
        }

        GUILayout.EndScrollView();
    }

    private void StartLogCat()
    {
        if( logCatProcess != null )
            StopLogCat();

        // Start `adb logcat` (with additional optional arguments) process for filtering
        ProcessStartInfo logProcessInfo = new ProcessStartInfo();
        logProcessInfo.CreateNoWindow = true;
        logProcessInfo.UseShellExecute = false;
        logProcessInfo.RedirectStandardOutput = true;
        logProcessInfo.RedirectStandardError = true;
        logProcessInfo.FileName = EditorPrefs.GetString( "AndroidSdkRoot" ) + "/platform-tools/adb";
        logProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;

        // Add additional -s argument for filtering by Unity tag.
        logProcessInfo.Arguments = "logcat" + ( prefilterOnlyUnity ? " -s Unity" : "" );

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

    private void AddLog( LogCatLog log )
    {
        lock ( waitingLogs )
        {
            waitingLogs.Enqueue( log );
        }
    }

    private struct LogCatLog
    {
        public readonly char Type;
        public readonly string Message;

        public LogCatLog( string data )
        {
            // First char indicates error type:
            // W - warning
            // E - error
            // D - debug
            // I - info
            // V - verbose
            Type = data[0];

            Message = DateTime.Now.ToString( "H:mm:ss" ) + " | " + data.Substring( 2 );
        }

        public Color GetBgColor()
        {
            switch( Type )
            {
                case 'D':
                    return Color.blue;
                case 'W':
                    return Color.yellow;
                case 'I':
                    return Color.green;
                case 'E':
                    return Color.red;
                case 'V':
                default:
                    return Color.grey;
            }
        }
    }
}
