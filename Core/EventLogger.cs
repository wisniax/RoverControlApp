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
			LogMessage($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()} Hello on startup :)");
			LogMessage($"Operating system:  --> {Environment.OSVersion.VersionString}");
			LogMessage($"Process path:      --> {Environment.ProcessPath}");
			LogMessage($"Program directory: --> {Environment.CurrentDirectory}");
			LogMessage($"Config directory:  --> {OS.GetUserDataDir()}");
		}

		public void LogMessage(string str)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append((DateTime.Now - _appStartedTimestamp).TotalSeconds.ToString("f4", new CultureInfo("en-US")));
			sb.Append($" -{Thread.CurrentThread.Name ?? Environment.CurrentManagedThreadId.ToString()}- ");
			sb.Append(str);
			GD.Print(sb.ToString());
		}
	}
}
