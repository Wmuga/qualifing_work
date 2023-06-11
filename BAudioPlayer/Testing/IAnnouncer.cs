using System;
using System.Collections.Generic;
using System.Text;

namespace BAudioPlayer.Testing
{
	public delegate void AnnounceEventHandler(string lib, string method);
	internal interface IAnnouncer
	{
		public event AnnounceEventHandler Announce;
	}
}
