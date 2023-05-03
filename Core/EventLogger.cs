using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Environment = System.Environment;

namespace RoverControlApp.Core
{
	public class EventLogger
	{
		private readonly DateTime _appStartedTimestamp;

		public EventLogger()
		{
			_appStartedTimestamp = DateTime.Now;
			PrintOnStartup();
		}

		private void PrintOnStartup()
		{
			LogMessege($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()} Hello on startup :)");
			LogMessege($"Operating system:  --> {Environment.OSVersion.VersionString}");
			LogMessege($"Process path:      --> {Environment.ProcessPath}");
			LogMessege($"Program directory: --> {Environment.CurrentDirectory}");
			LogMessege($"Config directory:  --> {OS.GetUserDataDir()}");
		}

		public void LogMessege(string str)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append((DateTime.Now - _appStartedTimestamp).TotalSeconds.ToString("f4", new CultureInfo("en-US")));
			sb.Append($" -{Thread.CurrentThread.Name ?? Environment.CurrentManagedThreadId.ToString()}- ");
			sb.Append(str);
			GD.Print(sb.ToString());
		}
	}
}
