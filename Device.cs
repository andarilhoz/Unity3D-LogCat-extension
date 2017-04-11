using System;
using UnityEngine;

namespace LogCatExtension
{
	internal class Device
	{
		public readonly string Name;
		public readonly string ID;

		public Device( string data )
		{
			ID = data.Substring(0, data.IndexOf(" "));
			Name = data.Substring(data.LastIndexOf(":")).TrimStart();
		}
	}
}