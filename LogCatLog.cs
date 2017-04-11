using System;
using UnityEngine;

namespace LogCatExtension
{
	internal class LogCatLog
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

