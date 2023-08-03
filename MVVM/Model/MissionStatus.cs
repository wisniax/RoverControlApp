﻿using System;
using System.ServiceModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GodotPlugins.Game;
using RoverControlApp.Core;
using RoverControlApp.MVVM.ViewModel;

namespace RoverControlApp.MVVM.Model
{
	public class MissionStatus
	{
		public event Func<MqttClasses.RoverMissionStatus?, Task>? OnRoverMissionStatusChanged;

		private CancellationTokenSource _cts;
		private Thread? _retriveMisionStatusThread;
		private MqttClient? _mqttClient => MainViewModel.MqttClient;
		private LocalSettings.Mqtt? _mqttSettings => MainViewModel.Settings?.Settings?.Mqtt;

		private MqttClasses.RoverMissionStatus? _status;
		public MqttClasses.RoverMissionStatus? Status
		{
			get => _status;
			private set
			{
				_status = value;
				MainViewModel.EventLogger?.LogMessage($"Mission status set to: {value?.MissionStatus} at " +
													  $"{DateTimeOffset.FromUnixTimeMilliseconds(value?.Timestamp ?? 0)}");
				OnRoverMissionStatusChanged?.Invoke(value);
			}
		}

		public MissionStatus()
		{
			_cts = new CancellationTokenSource();
			_retriveMisionStatusThread = new Thread(ThreadWork) { IsBackground = true, Name = "RetriveMisionStatusThread", Priority = ThreadPriority.BelowNormal };
			_retriveMisionStatusThread.Start();
			//if (!TryRetrieveOldStatus())
			//	Status = new MqttClasses.RoverMissionStatus() { MissionStatus = MqttClasses.MissionStatus.Created };
		}

		private void ThreadWork()
		{
			MainViewModel.EventLogger?.LogMessage("MissionStatus: Retrieving status in progress");
			string? serialized = "";
			SpinWait.SpinUntil(() => _mqttClient?.ConnectionState == CommunicationState.Opened);
			SpinWait.SpinUntil(() =>
			{
				serialized = _mqttClient?.GetReceivedMessageOnTopic(_mqttSettings?.TopicMissionStatus);
				return serialized != null;
			}, 5000);

			MqttClasses.RoverMissionStatus? status;

			try
			{
				status = JsonSerializer.Deserialize<MqttClasses.RoverMissionStatus>(serialized);
			}
			catch (Exception e)
			{
				MainViewModel.EventLogger?.LogMessage($"MissionStats: Error caught {e}");
				Status = new MqttClasses.RoverMissionStatus();
				return;
			}
			if (status == null)
				Status = new MqttClasses.RoverMissionStatus();
			Status = status;

			MainViewModel.EventLogger?.LogMessage("MQTT: Retrieving status succeeded");
		}

		private bool TryRetrieveOldStatus()
		{
			return false;
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
