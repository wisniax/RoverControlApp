using Godot;
using RoverControlApp.MVVM.Model;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using Environment = System.Environment;

namespace RoverControlApp.Core
{
	public static class EventLogger
	{
		public enum LogLevel
		{
			None,
			Verbose,
			Info,
			Warning,
			Error,
			CriticalError
		}


		private static Stopwatch _appRunningTimer = Stopwatch.StartNew();

		static EventLogger()
		{
			Thread.CurrentThread.Name = "MainUI_Thread";
			PrintOnStartup();
		}

		private static void PrintOnStartup()
		{
			LogMessage(String.Empty, LogLevel.None ,$"====================");
			LogMessage(String.Empty, LogLevel.None ,$"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()} Hello on startup :)");
			LogMessage(String.Empty, LogLevel.None ,$"Operating system:  --> {Environment.OSVersion.VersionString}");
			LogMessage(String.Empty, LogLevel.None ,$".NET Version:      --> {System.Runtime.InteropServices.RuntimeEnvironment.GetSystemVersion()}");
			LogMessage(String.Empty, LogLevel.None ,$"Process path:      --> {Environment.ProcessPath}");
			LogMessage(String.Empty, LogLevel.None ,$"Program directory: --> {Environment.CurrentDirectory}");
			LogMessage(String.Empty, LogLevel.None ,$"Config directory:  --> {OS.GetUserDataDir()}");
			LogMessage(String.Empty, LogLevel.None, $"====================\n");
		}

		[Obsolete("Consider using LogMessage with LogLevel, to see stack trace in Godot Editor on warnings and errors!", false)]
		public static void LogMessage(string str)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append($"<{(_appRunningTimer.Elapsed).TotalSeconds.ToString("f4", new CultureInfo("en-US"))}>");
			sb.Append($" -{Thread.CurrentThread.Name ?? Environment.CurrentManagedThreadId.ToString()}- ");
			sb.Append(str);
			GD.Print(sb.ToString());
		}

		public static void LogMessage(string source, LogLevel level, string message)
		{
			if (level == LogLevel.Verbose && LocalSettings.Singleton?.General.VerboseDebug == false) return;

			StringBuilder sb = new StringBuilder();
			sb.Append($"<{(_appRunningTimer.Elapsed).TotalSeconds.ToString("f4", new CultureInfo("en-US"))}>");
			if (level != LogLevel.None)
			{
				sb.Append($" -{Thread.CurrentThread.Name ?? Environment.CurrentManagedThreadId.ToString()}-");
				sb.Append($" [{source}]");
				sb.Append($" ({level}):");
			}
			sb.Append($" {message}");

			switch (level)
			{
				case LogLevel.None:
				case LogLevel.Verbose:
				case LogLevel.Info:
					GD.Print(sb.ToString());
					break;
				case LogLevel.Warning:
					GD.PushWarning($"\n{sb.ToString()}");
					break;
				case LogLevel.Error: 
				case LogLevel.CriticalError:
					GD.PushError($"\n{sb.ToString()}");
					break;
			}
		}

		public static void LogMessageDebug(string source, LogLevel level, string message)
		{
#if DEBUG
			LogMessage(source, level, $"(DEBUG) {message}");
#endif
		}
	}
}
