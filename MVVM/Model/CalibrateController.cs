using MQTTnet.Protocol;
using RoverControlApp.Core;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.Model;

public static class CalibrateController
{
	// Velocity Managers
	private static VelocityManager? _velocityManager = null;
	private static readonly object _velocityManagerLock = new();

	private static int _lastActionInt = (int)LastActions.None;
	public const int _intervalMs = 200;

	public static LastActions LastAction => (LastActions)Volatile.Read(ref _lastActionInt);

	// Changing the Last Actions
	private static void ChangeLastAction(int action)
	{
		int prevInt = Interlocked.Exchange(ref _lastActionInt, action);
		var prevAction = (LastActions)prevInt;
		EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Verbose,
			$"LastAction changed: {prevAction} -> {(LastActions)action}");
	}

	// Maping CalibrateAxisAction to the LastActions
	private static LastActions MapActionToLastAction(MqttClasses.CalibrateAxisAction mqttAction)
	{
		return mqttAction switch
		{
			MqttClasses.CalibrateAxisAction.Stop => LastActions.Action,
			MqttClasses.CalibrateAxisAction.ReturnToOrigin => LastActions.Action,
			MqttClasses.CalibrateAxisAction.Confirm => LastActions.Action,
			MqttClasses.CalibrateAxisAction.Cancel => LastActions.Action,
			MqttClasses.CalibrateAxisAction.Offset => LastActions.Offset,
			MqttClasses.CalibrateAxisAction.SetVelocity => LastActions.VelocityStarted,
			_ => LastActions.None
		};
	}

	// For Managing the Velocity, can by change by pad input
	private class VelocityManager
	{
		public CancellationTokenSource Cts;
		public Task? Worker;
		public volatile float Velocity;
		public readonly Guid OperationId;
		public readonly byte VescId;

		public VelocityManager(CancellationTokenSource cts, float initialVelocity, byte vescId)
		{
			Cts = cts;
			Velocity = initialVelocity;
			OperationId = Guid.NewGuid();
			VescId = vescId;
		}
	}

	// Last actions to help track and maintain conditions
	public enum LastActions
	{
		None,
		Action,
		Offset,
		VelocityStarted,
		VelocityStopped
	}

	// Main function to send the message via mqtt
	public static async Task<bool> SendCalibrateAxisAsync(
		MqttClasses.CalibrateAxisAction actionType,
		byte vescId,
		float value,
		long? timestamp = null,
		MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce,
		bool retain = false,
		bool updateLastAction = true)
	{
		try
		{
			if (updateLastAction)
			{
				var mapped = MapActionToLastAction(actionType);
				ChangeLastAction((int)mapped);
			}

			if (
				PressedKeys.Singleton.ControlMode != MqttClasses.ControlMode.EStop &&
				LastAction != LastActions.Action && LastAction != LastActions.None
			)
			{
				EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Warning,
					"ControlMode is not in EStop mode. Not sending CalibrateAxis.");
				return false;
			}

			var msg = new MqttClasses.CalibrateAxis
			{
				ActionType = actionType,
				VescID = vescId,
				Value = value,
				Timestamp = timestamp ?? DateTimeOffset.Now.ToUnixTimeMilliseconds()
			};

			string payload = JsonSerializer.Serialize(msg);

			EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Verbose,
				$"Preparing CalibrateAxis payload: {payload}");

			var node = MqttNode.Singleton;
			if (node is null)
			{
				EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Error,
					"MqttNode.Singleton is null. Cannot send CalibrateAxis.");
				return false;
			}

			bool enqueued = await node.EnqueueMessageAsync(
				LocalSettings.Singleton.Mqtt.TopicCalibrateControl, payload, qos, retain
			).ConfigureAwait(false);

			EventLogger.LogMessage(nameof(CalibrateController),
				enqueued ? EventLogger.LogLevel.Verbose : EventLogger.LogLevel.Warning,
				$"CalibrateAxis async enqueue result: {enqueued}");

			return enqueued;
		}
		catch (Exception ex)
		{
			EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Error,
				$"SendCalibrateAxisAsync exception: {ex}");
			return false;
		}
	}

	// Function for handling the actions
	public static async Task<bool> SendCalibrateActionAsync(
		MqttClasses.CalibrateAxisAction actionType,
		byte vescId,
		float value = 0f,
		long? timestamp = null,
		MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce,
		bool retain = false)
	{
		StopVelocity(); // Before making to stop velocity manager before sending an action
		return await SendCalibrateAxisAsync(actionType, vescId, value, timestamp, qos, retain).ConfigureAwait(false);
	}

	// Small functions for every button
	public static Task<bool> SendStopAsync(byte vescId) =>
		SendCalibrateActionAsync(MqttClasses.CalibrateAxisAction.Stop, vescId);

	public static Task<bool> SendReturnToOriginAsync(byte vescId) =>
		SendCalibrateActionAsync(MqttClasses.CalibrateAxisAction.ReturnToOrigin, vescId);

	public static Task<bool> SendConfirmAsync(byte vescId) =>
		SendCalibrateActionAsync(MqttClasses.CalibrateAxisAction.Confirm, vescId);

	public static Task<bool> SendCancelAsync(byte vescId) =>
		SendCalibrateActionAsync(MqttClasses.CalibrateAxisAction.Cancel, vescId);

	public static Task<bool> SendOffsetAsync(byte vescId, float value) =>
		SendCalibrateActionAsync(MqttClasses.CalibrateAxisAction.Offset, vescId, value);

	/// <summary>
	/// 
	/// </summary>
	public static bool StartVelocity(byte vescId, float initialVelocity, int intervalMs = _intervalMs)
	{
		if (PressedKeys.Singleton.ControlMode != MqttClasses.ControlMode.EStop) return false;

		lock (_velocityManagerLock)
		{
			if (IsVelocityRunning())
			{
				EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Warning,
					$"StartVelocity: worker already exists (vesc {vescId}). New start cancelled.");
				return false;
			}

			var cts = new CancellationTokenSource();
			var manager = new VelocityManager(cts, initialVelocity, vescId);

			ChangeLastAction((int)LastActions.VelocityStarted);

			manager.Worker = Task.Run(async () =>
			{
				var token = cts.Token;
				EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Verbose,
					$"StartVelocity: started for vesc {vescId} with initial velocity {initialVelocity}; op={manager.OperationId}");

				try
				{
					while (!token.IsCancellationRequested)
					{
						try
						{
							await SendCalibrateAxisAsync(MqttClasses.CalibrateAxisAction.SetVelocity, vescId, manager.Velocity)
								.ConfigureAwait(false);
						}
						catch (OperationCanceledException) { break; }
						catch (Exception ex)
						{
							EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Error,
								$"StartVelocity loop exception for vesc {vescId}; op={manager.OperationId}: {ex}");
						}

						try
						{
							await Task.Delay(intervalMs, token).ConfigureAwait(false);
						}
						catch (OperationCanceledException) { break; }
					}
				}
				finally
				{
					EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Verbose,
						$"StartVelocity: ended for vesc {vescId}; op={manager.OperationId}");

					bool managerStillPresent = false;
					lock (_velocityManagerLock)
					{
						if (ReferenceEquals(_velocityManager, manager))
						{
							managerStillPresent = true;
							_velocityManager = null;
						}
					}

					if (managerStillPresent)
					{
						try
						{
							ChangeLastAction((int)LastActions.VelocityStopped);
							await Task.Delay(intervalMs).ConfigureAwait(false);
							await SendCalibrateAxisAsync(MqttClasses.CalibrateAxisAction.SetVelocity, vescId, 0f, updateLastAction: false).ConfigureAwait(false);
						}
						catch (Exception)
						{
							EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Error,
								$"StartVelocity last send exception for vesc {vescId}; op={manager.OperationId}");
						}

						try { manager.Cts.Dispose(); } catch { }
					}
				}
			}, cts.Token);

			_velocityManager = manager;

			return true;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool IsVelocityRunning()
	{
		lock (_velocityManagerLock)
		{
			return _velocityManager != null;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool UpdateVelocity(float newVelocity)
	{
		lock (_velocityManagerLock)
		{
			if (_velocityManager is not null)
			{
				_velocityManager.Velocity = newVelocity;
				EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Verbose,
					$"UpdateVelocity: vesc {_velocityManager.VescId} -> {newVelocity}; op={_velocityManager.OperationId}");
				return true;
			}
		}

		EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Warning,
			$"UpdateVelocity: no active worker");
		return false;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool StopVelocity()
	{
		VelocityManager? manager;
		lock (_velocityManagerLock)
		{
			if (_velocityManager is null)
			{
				EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Warning,
					$"StopVelocity: no active worker");
				return false;
			}

			manager = _velocityManager;
			_velocityManager = null;
		}

		try
		{
			try
			{
				SendCalibrateAxisAsync(MqttClasses.CalibrateAxisAction.SetVelocity, manager.VescId, 0f, updateLastAction: false)
					.ConfigureAwait(false).GetAwaiter().GetResult();
			}
			catch (Exception ex)
			{
				EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Warning,
					$"StopVelocity: failed to send final zero for vesc {manager.VescId}: {ex.Message}; op={manager.OperationId}");
			}

			ChangeLastAction((int)LastActions.VelocityStopped);

			try { manager.Cts.Cancel(); } catch { }
			try { manager.Cts.Dispose(); } catch { }

			EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Verbose,
				$"StopVelocity: stop requested for vesc {manager.VescId}; op={manager.OperationId}");
			return true;
		}
		catch (Exception ex)
		{
			EventLogger.LogMessage(nameof(CalibrateController), EventLogger.LogLevel.Error,
				$"StopVelocity exception for vesc {manager.VescId}: {ex}");
			return false;
		}
	}

}
