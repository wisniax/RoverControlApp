using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoverControlApp.Core;
using RoverControlApp.MVVM.ViewModel;

namespace RoverControlApp.MVVM.Model
{
	public class MissionStatus
	{
		public event Func<MqttClasses.RoverMissionStatus?, Task>? OnRoverMissionStatusChanged;
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
			if (!TryRetriveOldStatus())
				Status = new MqttClasses.RoverMissionStatus() { MissionStatus = MqttClasses.MissionStatus.Created };
		}

		private bool TryRetriveOldStatus()
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
