using Godot;
using MQTTnet;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel
{
	public partial class RosoutLogs : Panel
	{

		private readonly List<MqttClasses.RosoutLogs> _allLogsHistory = new();
		private string _selectedNodeFilter = "All";

		public static event Action<int, Color>? OnNewLogReceived;

		private readonly Dictionary<int, bool> _activeFilters = new()
		{
			//DEBUG
			{ 10, false },
			//INFO
			{ 20, false },
			//WARN
			{ 30, true },
			//ERROR
			{ 40, true },
			//FATAL
			{ 50, true },
		};


		private readonly List<string> _hardcodedNodes = new()
		{
			"All",
			"manipulator_ws",
			"raptor_ws"
		};

		[Export]
		private RichTextLabel _logDisplay = null!;

		[Export]
		private OptionButton _selectNode = null!;

		public override void _EnterTree()
		{
			MqttNode.Singleton.MessageReceivedAsync += OnRosoutInfo;
			LocalSettings.Singleton.PropagatedPropertyChanged += OnSettingsPropertyChanged;
		}

		public override void _ExitTree()
		{
			MqttNode.Singleton.MessageReceivedAsync -= OnRosoutInfo;
			LocalSettings.Singleton.PropagatedPropertyChanged -= OnSettingsPropertyChanged;
		}

		void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
		{
			Callable.From(RebuildLogUI).CallDeferred();
		}

		public override void _Ready()
		{
			_logDisplay = GetNode<RichTextLabel>("LogsDisplay");
			_selectNode = GetNode<OptionButton>("SelectNode");

			foreach (string nodeName in _hardcodedNodes)
			{
				_selectNode.AddItem(nodeName);
			}

			_selectNode.Select(0);
		}

		private Task OnRosoutInfo(string subtopic, MqttApplicationMessage? msg)
		{
			if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicRosoutLogs))
				return Task.CompletedTask;
			string expectedPrefix = LocalSettings.Singleton.Mqtt.TopicRosoutLogs.Replace("+", "");
			if (!subtopic.StartsWith(expectedPrefix))
				return Task.CompletedTask;
			if (msg == null || msg.PayloadSegment.Count == 0)
			{
				EventLogger.LogMessage("RosoutLogs", EventLogger.LogLevel.Error, "Empty payload");
				return Task.CompletedTask;
			}

			try
			{
				var newLog = JsonSerializer.Deserialize<MqttClasses.RosoutLogs>(msg.ConvertPayloadToString());
				if (newLog == null)
					throw new InvalidDataException("Invalid RosoutLogs payload.");

				lock (_allLogsHistory)
				{
					_allLogsHistory.Add(newLog);

					if (_allLogsHistory.Count > 1000)
						_allLogsHistory.RemoveAt(0);
				}
				Callable.From(RebuildLogUI).CallDeferred();

				OnNewLogReceived?.Invoke(newLog.level,SetLogColor(newLog.level));
			}
			catch (Exception e)
			{
				EventLogger.LogMessage("RosoutLogs", EventLogger.LogLevel.Error, $"{e.Message}");

			}

			return Task.CompletedTask;
		}

		private void RebuildLogUI()
		{
			List<MqttClasses.RosoutLogs> logsToRender;

			lock (_allLogsHistory)
			{
				logsToRender = _allLogsHistory
					.Where(log =>
					_activeFilters.TryGetValue(log.level, out bool isActive) && isActive
					&& (_selectedNodeFilter == "All" || log.name == _selectedNodeFilter)
					)
					.ToList();
			}

			var sb = new StringBuilder();

			foreach (var log in logsToRender)
			{
				sb.Append($"[color={SetLogColor(log.level).ToHtml(false)}]");
				if (log.level == 50) sb.Append("[i][b]");

				var segments = new List<string>
				{
					DateTimeOffset.FromUnixTimeSeconds(log.Timestamp).LocalDateTime.ToString("HH:mm:ss.fff"),
					LevelIntToString(log.level),
					log.name
				};

				if (LocalSettings.Singleton.Rosout.Message) segments.Add(log.message);
				if (LocalSettings.Singleton.Rosout.File) segments.Add(log.file);
				if (LocalSettings.Singleton.Rosout.Function) segments.Add(log.function);
				if (LocalSettings.Singleton.Rosout.Line) segments.Add(log.line.ToString());


				sb.Append(string.Join(" : ", segments));
				if (log.level == 50) sb.Append("[/b][/i]");
				sb.Append("[/color]\n");
			}

			_logDisplay.Text = sb.ToString();
		}

		private void ToggleFilter(bool isPressed, int level)
		{
			if (_activeFilters.ContainsKey(level))
			{
				_activeFilters[level] = isPressed;
				Callable.From(RebuildLogUI).CallDeferred();
			}
		}

		private void OnNodeFilterItemSelected(int index)
		{
			_selectedNodeFilter = _selectNode.GetItemText(index);

			Callable.From(RebuildLogUI).CallDeferred();
		}

		private string LevelIntToString(int? level)
		{
			string val = "DEBUG";

			switch (level)
			{
				case 10:
					val = "DEBUG";
					break;
				case 20:
					val = "INFO";
					break;
				case 30:
					val = "WARN";
					break;
				case 40:
					val = "ERROR";
					break;
				case 50:
					val = "FATAL";
					break;
			}

			return val;
		}

		private Color SetLogColor(int? level)
		{
			Color color = Colors.White;

			switch (level)
			{
				case 10:
					color = Colors.Gray;
					break;
				case 20:
					color = Colors.White;
					break;
				case 30:
					color = Colors.Yellow;
					break;
				case 40:
					color = Colors.Red;
					break;
				case 50:
					color = Colors.Red;
					break;
			}

			return color;
		}
	}
	}
