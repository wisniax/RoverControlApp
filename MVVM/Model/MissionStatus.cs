using RoverControlApp.Core;
using Godot;
using System;
using System.ServiceModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.Model
{
	public partial class MissionStatus : Node
	{
		public event Func<MqttClasses.RoverMissionStatus?, Task>? OnRoverMissionStatusChanged;

		private CancellationTokenSource _cts = new CancellationTokenSource();
		private Thread? _retriveMisionStatusThread;

		private MqttClasses.RoverMissionStatus? _status;
		public MqttClasses.RoverMissionStatus? Status
		{
			get => _status;
			private set
			{
				_status = value;
				EventLogger.LogMessage("MissionStatus", EventLogger.LogLevel.Info, $"Mission status set to: {value?.MissionStatus} at " +
													  $"{DateTimeOffset.FromUnixTimeMilliseconds(value?.Timestamp ?? 0)}");
				OnRoverMissionStatusChanged?.Invoke(value);
			}
		}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public static MissionStatus Singleton { get; private set; }
#pragma warning restore CS8618

        /*
		*	Godot overrides
		*/

        public override void _Ready()
        {
            base._Ready();
			_retriveMisionStatusThread = new Thread(ThreadWork) { IsBackground = true, Name = "RetriveMisionStatusThread", Priority = ThreadPriority.BelowNormal };
			_retriveMisionStatusThread.Start();
			Singleton ??= this;
        }

        protected override void Dispose(bool disposing)
        {
			_cts.Cancel();
			Singleton = null!;
            base.Dispose(disposing);
        }

       /*
		*	Godot overrides end
		*/

		private void ThreadWork()
		{
			EventLogger.LogMessage("MissionStatus", EventLogger.LogLevel.Verbose, "Retrieving status in progress");
			string? serialized = "";
			SpinWait.SpinUntil(() => MqttNode.Singleton.ConnectionState == CommunicationState.Opened);
			SpinWait.SpinUntil(() =>
			{
				serialized = MqttNode.Singleton.GetReceivedMessageOnTopicAsString(LocalSettings.Singleton.Mqtt.TopicMissionStatus);
				return serialized != null;
			}, 5000);

			MqttClasses.RoverMissionStatus? status;

			try
			{
				status = JsonSerializer.Deserialize<MqttClasses.RoverMissionStatus>(serialized);
			}
			catch (Exception e)
			{
				EventLogger.LogMessage("MissionStatus", EventLogger.LogLevel.Error, $"Error caught {e}");
				Status = new MqttClasses.RoverMissionStatus();
				return;
			}

			if (status == null)
			{
				EventLogger.LogMessage("MissionStatus", EventLogger.LogLevel.Error, $"Null reference stopping mission.");
				Status = new MqttClasses.RoverMissionStatus();
				return;
			}

			var hoursPassed = (DateTime.Now - DateTimeOffset.FromUnixTimeMilliseconds(status.Timestamp).DateTime).TotalHours;
			if (hoursPassed > 8)
			{
				EventLogger.LogMessage("MissionStatus", EventLogger.LogLevel.Warning, $"Retrieving status succeeded but was older than 8 hours thus mission was stopped.");
				Status = new MqttClasses.RoverMissionStatus();
				return;
			}

			Status = status;
			EventLogger.LogMessage("MissionStatus", EventLogger.LogLevel.Info, $"MissionStatus: Retrieving status succeeded");
		}

		public void StopMission()
		{
			Status = new MqttClasses.RoverMissionStatus() { MissionStatus = MqttClasses.MissionStatus.Stopped };
		}

		public void StartMission()
		{
			Status = new MqttClasses.RoverMissionStatus() { MissionStatus = MqttClasses.MissionStatus.Started };
		}

		public void PauseMission()
		{
			Status = new MqttClasses.RoverMissionStatus() { MissionStatus = MqttClasses.MissionStatus.Interrupted };
		}
	}
}
