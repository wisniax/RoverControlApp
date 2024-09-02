using Godot;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using MQTTnet.Server;
using RoverControlApp.Core;
using RoverControlApp.Core.Settings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.ServiceModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.Model;

public partial class MqttNode : Node
{ 
	[Signal]
	public delegate void ConnectionChangedEventHandler(ConnectionState state);

	[Signal]
	public delegate void MessageReceivedEventHandler(string topic, MqttNodeMessage message);

	public delegate Task MessageReceivedAsyncEventHandler(string topic, MqttApplicationMessage? message);

	public event MessageReceivedAsyncEventHandler? MessageReceivedAsync;

	public CommunicationState ConnectionState
	{
		get => _connectionState;
		private set
		{
			if (_connectionState == value) return;
			_connectionState = value;
			CallDeferred(MethodName.EmitSignal, SignalName.ConnectionChanged, (int)_connectionState);
			EventLogger.LogMessageDebug("MqttNode", EventLogger.LogLevel.Verbose, $"Connection state changed to \"{value}\"");
		}
	}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public static MqttNode Singleton { get; private set; }
#pragma warning restore CS8618


	/*
	 * Godot overrides
	 */

	public override void _Ready()
	{
		Singleton ??= this;
		LocalSettings.Singleton.Connect(LocalSettings.SignalName.CategoryChanged, Callable.From<StringName>(OnSettingsCategoryChanged));
		LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
		LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedSubcategoryChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsSubcategoryChanged));
		MqStart();
	}

	public override void _ExitTree()
	{
		LocalSettings.Singleton.Disconnect(LocalSettings.SignalName.CategoryChanged, Callable.From<StringName>(OnSettingsCategoryChanged));
		LocalSettings.Singleton.Disconnect(LocalSettings.SignalName.PropagatedPropertyChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
		LocalSettings.Singleton.Disconnect(LocalSettings.SignalName.PropagatedSubcategoryChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsSubcategoryChanged));
		MqStop();
		RequestReady();
	}

	/*
	 * Settings handlers
	 */ 

	void OnSettingsCategoryChanged(StringName property)
	{
		if (property != nameof(LocalSettings.Mqtt)) return;

		MqRestart();
	}

	void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
	{
		if (category != nameof(Mqtt)) return;

		var (newTopic, newQos) =
			(LocalSettings.Singleton.Mqtt.GetAllTopicsToSubscribe().Where(entry => entry.Item1 == newValue.AsString())).FirstOrDefault();

		if (string.IsNullOrEmpty(newTopic))
			return;

		MqUnsubscribeTopic(oldValue.AsString());
		MqSubscribeTopic(newTopic, newQos);
	}

	void OnSettingsSubcategoryChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
	{
		if (category != nameof(Mqtt) || name != nameof(Mqtt.ClientSettings)) return;

		MqRestart();		
	}

	/*
	 * Helper methods
	 */

	public static string TopicFull(string subtopic)
	{
		return LocalSettings.Singleton.Mqtt.ClientSettings.TopicMain + "/" + subtopic;
	}

	/*
	 * Public methods Mqtt
	 */

	public async Task EnqueueMessageAsync(string subtopic, string? arg, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false)
	{
		if (subtopic == null || arg == null)
		{
			return;
		}
		
		await _managedMqttClient.EnqueueAsync(TopicFull(subtopic),arg, qos, retain);
		EventLogger.LogMessageDebug("MqttNode", EventLogger.LogLevel.Verbose, $"Message enqueued at subtopic: \"{subtopic}\" with:\n{arg}");
	}

	public void EnqueueMessage(string subtopic, string? arg, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false)
	{
		Task.Run(async () => await _managedMqttClient.EnqueueAsync(subtopic, arg, qos, retain));
		EventLogger.LogMessageDebug("MqttNode", EventLogger.LogLevel.Verbose, $"Message enqueued at subtopic: \"{subtopic}\" with:\n{arg}");
	}

	public MqttApplicationMessage? GetReceivedMessageOnTopic(string? subtopic)
	{
		if (subtopic == null) return null;

		MqttApplicationMessage? response = new();
		var succ = _responses?.TryGetValue(subtopic, out response) ?? false;
		return succ ? response : null;
	}

	public string? GetReceivedMessageOnTopicAsString(string? subtopic)
	{
		return GetReceivedMessageOnTopic(subtopic)?.ConvertPayloadToString();
	}

	/*
	 * Methods for Mqtt control
	 */

	private bool MqStart()
	{
		if (ConnectionState == CommunicationState.Created)
		{
			EventLogger.LogMessageDebug(LogSource, EventLogger.LogLevel.Warning, "Can't start, mqtt is already starting!");
			return false;
		}

		if (ConnectionState != CommunicationState.Closed)
		{
			EventLogger.LogMessageDebug(LogSource, EventLogger.LogLevel.Warning, "Can't start, mqtt is not fully closed!");
			return false;
		}

		EventLogger.LogMessage(LogSource, EventLogger.LogLevel.Verbose, "Starting mqtt");
		ConnectionState = CommunicationState.Created;

		_cts = new CancellationTokenSource();
		_responses = new Dictionary<string, MqttApplicationMessage?>();
		_mqttThread = new Thread(ThWork) { IsBackground = true, Name = "MqttThread", Priority = ThreadPriority.BelowNormal };
		_mqttThread.Start();
		return true;
	}

	private bool MqStop(bool awaitFullStop = false)
	{
		switch (ConnectionState)
		{
			case CommunicationState.Closed:
				EventLogger.LogMessageDebug(LogSource, EventLogger.LogLevel.Warning, "Can't stop, mqtt is stopped!");
				return false;
			case CommunicationState.Closing:
				EventLogger.LogMessageDebug(LogSource, EventLogger.LogLevel.Warning, "Can't stop, mqtt is already stopping!");
				return false;
		}

		EventLogger.LogMessageDebug(LogSource, EventLogger.LogLevel.Verbose, "Requesting thread stop");
		_cts!.Cancel();

		if(awaitFullStop)
		{
			_mqttThread!.Join();
			EventLogger.LogMessageDebug(LogSource, EventLogger.LogLevel.Verbose, "Thread stop confirmed!");
			return true;
		}

		EventLogger.LogMessageDebug(LogSource, EventLogger.LogLevel.Verbose,
			_mqttThread!.Join(200) ? "Thread stop confirmed!" : "Thread stop not confirmed. Proceeding");

		return true;
	}

	private void MqRestart()
	{
		Task.Run(() => { MqStop(true); MqStart(); });
	}

	private async Task MqSubscribeTopicAsync(string subtopic, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce)
	{
		if (string.IsNullOrEmpty(subtopic)) return;
		var topic = TopicFull(subtopic);
		EventLogger.LogMessage(LogSource,EventLogger.LogLevel.Verbose, $"Subscribing to topic: \"{topic}\"");
		await _managedMqttClient.SubscribeAsync(topic, qos);
	}

	private void MqSubscribeTopic(string subtopic, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce)
	{
		Task.Run(async () => await MqSubscribeTopicAsync(subtopic, qos));
	}

	private async Task MqUnsubscribeTopicAsync(string subtopic)
	{
		if (string.IsNullOrEmpty(subtopic)) return;
		var topic = TopicFull(subtopic);
		EventLogger.LogMessage(LogSource, EventLogger.LogLevel.Verbose, $"Unsubscribing from topic: \"{topic}\"");
		await _managedMqttClient.UnsubscribeAsync(topic);
	}

	private void MqUnsubscribeTopic(string subtopic)
	{
		Task.Run(async () => await MqUnsubscribeTopicAsync(subtopic));
	}

	private async Task MqSubscribeAllAsync()
	{
		EventLogger.LogMessage(LogSource, EventLogger.LogLevel.Verbose, $"Subscribing to ALL topics.");

		List<Task> subTasks = [];

		foreach(var (topic, qos) in LocalSettings.Singleton.Mqtt.GetAllTopicsToSubscribe())
			subTasks.Add(MqSubscribeTopicAsync(topic, qos));

		await Task.WhenAll(subTasks);
	}

	/*
	 * Methods used only on thread
	 */

	private void ThWork()
	{
		EventLogger.LogMessage(LogSource, EventLogger.LogLevel.Verbose, "Thread started");
		ConnectionState = CommunicationState.Opening;
		
		//kind of worried about this one
		ThClientConnect().Wait();
		SpinWait.SpinUntil(() => _cts!.IsCancellationRequested);

		EventLogger.LogMessage(LogSource, EventLogger.LogLevel.Verbose, "Thread stopping");
		ConnectionState = CommunicationState.Closing;

		//and this
		ThClientDisconnect().Wait();
		EventLogger.LogMessage(LogSource, EventLogger.LogLevel.Verbose, "Thread quit");
		ConnectionState = CommunicationState.Closed;
	}

	private async Task ThClientConnect()
	{
		if (_managedMqttClient is not null)
			return;

		var mqttFactory = new MqttFactory();

		_managedMqttClient = mqttFactory.CreateManagedMqttClient();

		var mqttClientOptions = new MqttClientOptionsBuilder()
			.WithTcpServer(LocalSettings.Singleton.Mqtt.ClientSettings.BrokerIp, LocalSettings.Singleton.Mqtt.ClientSettings.BrokerPort)
			.WithKeepAlivePeriod(TimeSpan.FromSeconds(LocalSettings.Singleton.Mqtt.ClientSettings.PingInterval))
			.WithWillTopic(TopicFull(LocalSettings.Singleton.Mqtt.TopicRoverStatus))
			.WithWillPayload(JsonSerializer.Serialize(new MqttClasses.RoverStatus() { CommunicationState = CommunicationState.Faulted }))
			.WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
			.WithWillRetain()
			.Build();

		var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
			.WithClientOptions(mqttClientOptions)
			.WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
			.WithMaxPendingMessages(99)
			.WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
			.Build();

		_managedMqttClient.DisconnectedAsync += ThOnDisconnectedAsync;
		_managedMqttClient.ConnectedAsync += ThOnConnectedAsync;
		_managedMqttClient.SynchronizingSubscriptionsFailedAsync += ThOnSynchronizingSubscriptionsFailedAsync;
		_managedMqttClient.ApplicationMessageReceivedAsync += ThOnApplicationMessageReceivedAsync;

		await _managedMqttClient.StartAsync(managedMqttClientOptions);


		await MqSubscribeAllAsync();
	}

	private async Task ThClientDisconnect()
	{
		if (_managedMqttClient is null)
			return;

		_managedMqttClient.DisconnectedAsync -= ThOnDisconnectedAsync;
		_managedMqttClient.ConnectedAsync -= ThOnConnectedAsync;
		_managedMqttClient.SynchronizingSubscriptionsFailedAsync -= ThOnSynchronizingSubscriptionsFailedAsync;
		_managedMqttClient.ApplicationMessageReceivedAsync -= ThOnApplicationMessageReceivedAsync;

		await _managedMqttClient.EnqueueAsync
		(
			TopicFull(LocalSettings.Singleton.Mqtt.TopicRoverControl),
			JsonSerializer.Serialize(new MqttClasses.RoverControl())
		);

		await _managedMqttClient.EnqueueAsync
		(
			TopicFull(LocalSettings.Singleton.Mqtt.TopicManipulatorControl),
			JsonSerializer.Serialize(new MqttClasses.ManipulatorControl())
		);

		await _managedMqttClient.EnqueueAsync
		(
			TopicFull(LocalSettings.Singleton.Mqtt.TopicRoverStatus),
			JsonSerializer.Serialize(new MqttClasses.RoverStatus() { CommunicationState = CommunicationState.Closed }),
			MqttQualityOfServiceLevel.ExactlyOnce,
			true
		);

		await Task.Run(async Task? () =>
		{
			for (int i = 0; (_managedMqttClient.PendingApplicationMessagesCount > 0) && (i < 10); i++)
			{
				await Task.Delay(TimeSpan.FromMilliseconds(100));
			}
		});

		await _managedMqttClient?.StopAsync(_managedMqttClient.PendingApplicationMessagesCount == 0)!;
		SpinWait.SpinUntil(() => _managedMqttClient.IsConnected, 250);
		_managedMqttClient.Dispose();
		_managedMqttClient = null;

	}

	private Task ThOnConnectedAsync(MqttClientConnectedEventArgs arg)
	{
		EventLogger.LogMessage(LogSource, EventLogger.LogLevel.Info, "Mqtt is now Connected!");
		ConnectionState = CommunicationState.Opened;
		return Task.CompletedTask;
	}

	private Task ThOnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
	{
		EventLogger.LogMessage(LogSource, EventLogger.LogLevel.Info, "Mqtt is now Disconnected!");
		ConnectionState = CommunicationState.Faulted;
		return Task.CompletedTask;
	}

	private Task ThOnSynchronizingSubscriptionsFailedAsync(ManagedProcessFailedEventArgs arg)
	{
		EventLogger.LogMessage(LogSource, EventLogger.LogLevel.Error, $"Synchronizing subscriptions failed with: {arg}");
		return Task.CompletedTask;
	}

	private Task ThOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
	{
		EventLogger.LogMessageDebug(LogSource, EventLogger.LogLevel.Verbose, $"Message received on topic {arg.ApplicationMessage.Topic} with:\n   {arg.ApplicationMessage.ConvertPayloadToString()}");

		if (_responses == null) return Task.CompletedTask;

		var topic = arg.ApplicationMessage.Topic[(LocalSettings.Singleton.Mqtt.ClientSettings.TopicMain.Length + 1)..];
		var payload = arg.ApplicationMessage;

		if (_responses.ContainsKey(topic))
			_responses[topic] = payload;
		else if (!_responses.TryAdd(topic, payload))
			EventLogger.LogMessage(LogSource, EventLogger.LogLevel.Error, $"Adding {payload} on topic {topic} to dictionary failed");

		CallDeferred(MethodName.EmitSignal, SignalName.MessageReceived, topic, new MqttNodeMessage(payload));
		MessageReceivedAsync?.Invoke(topic, payload);

		return Task.CompletedTask;
	}

	/*
	 * Private members
	 */

	private IManagedMqttClient? _managedMqttClient;
	private CancellationTokenSource? _cts;
	private Thread? _mqttThread;

	private Dictionary<string, MqttApplicationMessage?>? _responses;

	private volatile CommunicationState _connectionState = CommunicationState.Closed;

	const string LogSource = "MqttNode";
}
