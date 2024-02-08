using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using Godot;

using Environment = System.Environment;

namespace RoverControlApp.Core
{
	public static class EventLogger
	{
		private static Stopwatch _appRunningTimer = Stopwatch.StartNew();

		static EventLogger()
		{
			Thread.CurrentThread.Name = "MainUI_Thread";
			PrintOnStartup();
		}

		private static void PrintOnStartup()
		{
			LogMessage($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()} Hello on startup :)");
			LogMessage($"Operating system:  --> {Environment.OSVersion.VersionString}");
			LogMessage($".NET Version:      --> {System.Runtime.InteropServices.RuntimeEnvironment.GetSystemVersion()}");
			LogMessage($"Process path:      --> {Environment.ProcessPath}");
			LogMessage($"Program directory: --> {Environment.CurrentDirectory}");
			LogMessage($"Config directory:  --> {OS.GetUserDataDir()}");
		}

		public static void LogMessage(string str)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append((_appRunningTimer.Elapsed).TotalSeconds.ToString("f4", new CultureInfo("en-US")));
			sb.Append($" -{Thread.CurrentThread.Name ?? Environment.CurrentManagedThreadId.ToString()}- ");
			sb.Append(str);
			GD.Print(sb.ToString());
		}
	}
}
