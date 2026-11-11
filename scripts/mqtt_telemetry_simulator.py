"""Send sample battery, VESC and IMU quaternion messages to RoverControlApp.

Install: python -m pip install -r scripts/requirements.txt
Run:     python scripts/mqtt_telemetry_simulator.py
Preview: python scripts/mqtt_telemetry_simulator.py --dry-run --count 1
"""

import argparse
import json
import math
import os
from pathlib import Path
import sys
import threading
import time


MANIPULATOR_AXES = (
    (0x70, -110.0, 110.0),  # Axis 1
    (0x71, -20.0, 110.0),   # Axis 2
    (0x72, -70.0, 40.0),    # Axis 3
    (0x73, -180.0, 180.0),  # Axis 4
    (0x74, -120.0, 120.0),  # Axis 5
    (0x75, -180.0, 180.0),  # Axis 6
)


def quaternion_from_euler(roll, pitch, yaw):
    """Unit quaternion (x, y, z, w) for Rz(yaw) * Ry(pitch) * Rx(roll), in radians."""
    sr, cr = math.sin(roll / 2), math.cos(roll / 2)
    sp, cp = math.sin(pitch / 2), math.cos(pitch / 2)
    sy, cy = math.sin(yaw / 2), math.cos(yaw / 2)
    values = (sr * cp * cy - cr * sp * sy,
              cr * sp * cy + sr * cp * sy,
              cr * cp * sy - sr * sp * cy,
              cr * cp * cy + sr * sp * sy)
    norm = math.sqrt(sum(value * value for value in values))
    return dict(zip(("x", "y", "z", "w"), (value / norm for value in values)))


def quaternion_without_yaw(quaternion):
    """Match IMU_WithoutYaw in VescGyro/Core/Src/imu.c (body-to-world ZYX)."""
    x, y, z, w = (quaternion[key] for key in ("x", "y", "z", "w"))
    yaw = math.atan2(2 * (w * z + x * y), 1 - 2 * (y * y + z * z))
    c, s = math.cos(yaw / 2), math.sin(yaw / 2)
    # Remove world-Z rotation on the left: q_tilt = q_z(-yaw) * q.
    values = (c * x + s * y, c * y - s * x, c * z - s * w, c * w + s * z)
    norm = math.sqrt(sum(value * value for value in values))
    if norm > 0:
        values = tuple(value / norm for value in values)
    return dict(zip(("x", "y", "z", "w"), (min(1.0, max(-1.0, value)) for value in values)))


def sample_imu_messages(elapsed, imu_ids, timestamp=None):
    """Frames matching MqttClasses.ImuBoardFeedback, with a Unix-ms timestamp."""
    if timestamp is None:
        timestamp = time.time_ns() // 1_000_000
    for index, can_id in enumerate(imu_ids):
        if can_id is None:  # Body IMU can be unconfigured in app settings.
            continue
        phase = index * 0.35
        roll_amplitude = math.radians(8 if index == 4 else 15)
        pitch_amplitude = math.radians(10)
        yaw_amplitude = math.radians(25)
        roll = roll_amplitude * math.sin(elapsed / 4 + phase)
        pitch = pitch_amplitude * math.sin(elapsed / 5 + phase)
        yaw = yaw_amplitude * math.sin(elapsed / 8 + phase)
        quaternion = quaternion_without_yaw(quaternion_from_euler(roll, pitch, yaw))
        yield "imu", {
            "VescID": can_id,
            "W": quaternion["w"],
            "X": quaternion["x"],
            "Y": quaternion["y"],
            "Z": quaternion["z"],
            "Timestamp": timestamp,
        }, 0


def parse_args():
    parser = argparse.ArgumentParser(description=__doc__)
    settings_source = parser.add_mutually_exclusive_group()
    settings_source.add_argument("--settings", type=Path, help="RoverControlAppSettings.json (auto-detected on Windows)")
    settings_source.add_argument("--defaults", action="store_true", help="Ignore saved settings and use app defaults")
    parser.add_argument("--host", help="MQTT broker (default: broker.hivemq.com)")
    parser.add_argument("--port", type=int, help="MQTT port (default: 1883, TLS: 8883)")
    parser.add_argument("--topic-main", help="Topic prefix (default: RappTORS)")
    parser.add_argument("--battery-topic", help="Battery subtopic (default: BatteryInfo)")
    parser.add_argument("--wheel-topic", help="Wheel subtopic (default: VescStatus)")
    parser.add_argument("--imu-topic", help="IMUBoard subtopic (default: ImuBoardFeedback)")
    parser.add_argument("--imu-ids", nargs=5, metavar=("FL", "FR", "RR", "RL", "BODY"),
                        help="IMUBoard IDs, decimal or 0x hex (default: 0xB0 0xB1 0xB2 0xB3 0xB4)")
    parser.add_argument("--steering-ids", nargs=4, metavar=("FL", "FR", "RR", "RL"),
                        help="VESC IDs, decimal or 0x hex (default: 0x60 0x61 0x62 0x63)")
    parser.add_argument("--gripper-range", nargs=2, type=float, default=(0.0, 1.0), metavar=("MIN", "MAX"),
                        help="Simulated gripper position range (default: 0 1)")
    parser.add_argument("--username", help="MQTT username")
    parser.add_argument("--password", help="MQTT password (or MQTT_PASSWORD env variable)")
    parser.add_argument("--tls", action="store_true", default=None, help="Enable TLS")
    parser.add_argument("--ca-cert", help="CA certificate path for TLS")
    parser.add_argument("--interval", type=float, default=0.05, help="Seconds per cycle (default: 0.05, 20 Hz)")
    parser.add_argument("--count", type=int, default=0, help="Number of cycles (0: until Ctrl+C)")
    parser.add_argument("--dry-run", action="store_true", help="Print JSON without connecting")
    args = parser.parse_args()
    if not math.isfinite(args.interval) or args.interval <= 0 or args.count < 0:
        parser.error("--interval must be positive and finite; --count must be >= 0")
    if not all(math.isfinite(value) for value in args.gripper_range) or args.gripper_range[0] >= args.gripper_range[1]:
        parser.error("--gripper-range requires finite MIN < MAX")

    if args.settings is None and not args.defaults and os.name == "nt":
        app_data = os.getenv("APPDATA")
        if app_data:
            candidate = Path(app_data) / "Godot" / "app_userdata" / "Rover Control App" / "RoverControlAppSettings.json"
            if candidate.is_file():
                args.settings = candidate

    settings = {}
    if args.settings:
        try:
            settings = json.loads(args.settings.read_text(encoding="utf-8-sig"))
        except (OSError, ValueError) as exc:
            parser.error(f"Cannot read settings: {exc}")
    mqtt = settings.get("Mqtt") or {}
    client = mqtt.get("ClientSettings") or {}
    wheels = settings.get("WheelData") or {}
    # Explicit CLI options override saved app settings, then app defaults.
    args.host = args.host if args.host is not None else client.get("BrokerIp", "broker.hivemq.com")
    args.tls = args.tls if args.tls is not None else client.get("UseTls", False)
    args.port = args.port if args.port is not None else client.get("BrokerPort", 8883 if args.tls else 1883)
    args.topic_main = args.topic_main if args.topic_main is not None else client.get("TopicMain", "RappTORS")
    args.battery_topic = args.battery_topic if args.battery_topic is not None else mqtt.get("TopicBatteryInfo", "BatteryInfo")
    args.wheel_topic = args.wheel_topic if args.wheel_topic is not None else mqtt.get("TopicWheelFeedback", "VescStatus")
    configured_imu_topic = mqtt.get("TopicImuBoardFeedback")
    args.imu_topic = args.imu_topic if args.imu_topic is not None else ("ImuBoardFeedback" if configured_imu_topic is None else configured_imu_topic)
    args.username = args.username if args.username is not None else client.get("Username", "")
    args.password = args.password if args.password is not None else os.getenv("MQTT_PASSWORD", client.get("Password", ""))
    args.ca_cert = args.ca_cert if args.ca_cert is not None else client.get("CertificateAuthorityCertPath")
    cli_ids = args.steering_ids is not None
    ids = args.steering_ids or [wheels.get(name, default) for name, default in (
        ("FrontLeftTurn", "0x60"), ("FrontRightTurn", "0x61"),
        ("BackRightTurn", "0x62"), ("BackLeftTurn", "0x63"),
    )]
    try:
        # WheelData settings use hex even without 0x, matching VelMonitor.
        args.steering_ids = [int(value, 16 if not cli_ids or value.lower().startswith("0x") else 10) for value in ids]
    except (ValueError, AttributeError):
        parser.error("VESC IDs must be decimal numbers or hex numbers starting with 0x")
    if len(set(args.steering_ids)) != 4 or any(not 0 <= value <= 255 for value in args.steering_ids):
        parser.error("Provide four distinct VESC IDs in the range 0..255")
    if any(0x70 <= value <= 0x76 for value in args.steering_ids):
        parser.error("Steering IDs cannot overlap manipulator IDs 0x70..0x76")
    imu_settings = (settings.get("CanIds") or {}).get("ImuBoards") or {}
    cli_imu_ids = args.imu_ids is not None
    imu_ids = args.imu_ids or [imu_settings.get(name, default) for name, default in (
        ("FrontLeft", "0xB0"), ("FrontRight", "0xB1"),
        ("RearRight", "0xB2"), ("RearLeft", "0xB3"), ("Body", "0xB4"),
    )]
    try:
        args.imu_ids = [None if index == 4 and value == "" and not cli_imu_ids
                        else int(value, 16 if not cli_imu_ids or value.lower().startswith("0x") else 10)
                        for index, value in enumerate(imu_ids)]
    except (ValueError, AttributeError, TypeError):
        parser.error("IMU IDs must be decimal numbers or hex numbers starting with 0x")
    active_imu_ids = [value for value in args.imu_ids if value is not None]
    if len(set(active_imu_ids)) != len(active_imu_ids) or any(not 0 <= value <= 255 for value in active_imu_ids):
        parser.error("IMU IDs must be distinct and in the range 0..255")
    if not args.host or not 1 <= args.port <= 65535:
        parser.error("Provide a broker host and port in the range 1..65535")
    for topic in (args.battery_topic, args.wheel_topic, args.imu_topic):
        full_topic = args.topic_main + "/" + topic  # Same as MqttNode.TopicFull.
        if not topic or any(char in full_topic for char in ("+", "#", "\x00")):
            parser.error("Topics must be nonempty and cannot contain +, # or NUL")
    return args


def sample_messages(elapsed, steering_ids, gripper_range=(0.0, 1.0), imu_ids=(0xB0, 0xB1, 0xB2, 0xB3, 0xB4)):
    """Yield (subtopic kind, JSON payload, QoS), using the C# property names."""
    timestamp = time.time_ns() // 1_000_000
    for slot in range(1, 4):
        charge = 75 + 15 * math.sin(elapsed / 30 + slot)
        yield "battery", {
            "Status": 2,  # BatteryStatus.Discharging; enums are numeric in JSON.
            "HotswapStatus": 7,  # Hotswap1 | Hotswap2 | Hotswap3 closed.
            "ID": 100 + slot,
            "Slot": slot - 1,  # BatteryInfo slots are zero-based: 0, 1, 2.
            "ChargePercent": round(charge, 1),
            "Current": round(-2.5 - 0.5 * math.sin(elapsed + slot), 2),
            "Temperature": round(29 + slot + math.sin(elapsed / 10), 1),
            "Voltage": round(21 + 4.2 * charge / 100, 2),
            "Timestamp": timestamp,
        }, 2

    for index, vesc_id in enumerate(steering_ids):
        # Front and rear axles turn in opposite directions; angles are degrees.
        direction = 1 if index < 2 else -1
        angle = direction * 35 * math.sin(elapsed / 3)
        target = direction * 35 * math.sin((elapsed + 0.2) / 3)
        yield "wheel", {
            "ERPM": round(direction * 80 * math.cos(elapsed / 3)),
            "VescId": vesc_id,  # JSON number: 0x60 becomes 96.
            "ADC1": 0.0, "ADC2": 0.0, "ADC3": 0.0,
            "AhCharged": 0.0, "AhUsed": round(elapsed * 0.0003, 6),
            "Current": round(1.5 + abs(math.cos(elapsed / 3)), 2),
            "CurrentIn": 1.2,
            "DutyCycle": round(0.1 * math.cos(elapsed / 3), 3),
            "PPM": 0.0,
            "PidPos": round(target, 2),
            "PrecisePos": round(angle, 2),
            "Tachometer": 0.0,
            "TempFet": round(32 + index + math.sin(elapsed / 10), 1),
            "TempMotor": round(35 + index + math.sin(elapsed / 10), 1),
            "VoltsIn": 24.0,
            "WhCharged": 0.0, "WhUsed": round(elapsed * 0.0072, 6),
            "Timestamp": timestamp,
        }, 0

    axes = MANIPULATOR_AXES + ((0x76, *gripper_range),)
    for index, (vesc_id, minimum, maximum) in enumerate(axes):
        midpoint = (minimum + maximum) / 2
        amplitude = (maximum - minimum) / 2
        phase = elapsed / 6 + index * 0.4
        position = midpoint + amplitude * math.sin(phase)
        target = midpoint + amplitude * math.sin(phase + 0.2 / 6)
        speed = math.cos(phase)
        load = abs(speed)
        current = 0.8 + (2.0 + index * 0.3) * load
        current_in = current * 0.35
        # Synthetic VESC telemetry, including all WheelFeedback fields.
        yield "wheel", {
            "ERPM": round(120 * speed),
            "VescId": vesc_id,
            "ADC1": round(1.65 * (1 + math.sin(phase)), 3),
            "ADC2": round(1.65 * (1 + math.cos(phase)), 3),
            "ADC3": round(3.3 * load, 3),
            "AhCharged": 0.0,
            "AhUsed": round(elapsed * (0.8 + index * 0.1) / 3600, 6),
            "Current": round(current, 2),
            "CurrentIn": round(current_in, 2),
            "DutyCycle": round(0.25 * speed, 3),
            "PPM": round(speed, 3),
            "PidPos": min(maximum, max(minimum, round(target, 3))),
            "PrecisePos": min(maximum, max(minimum, round(position, 3))),
            "Tachometer": round((position - midpoint) / 360 * 42, 3),
            "TempFet": round(33 + index + 2 * math.sin(elapsed / 20 + index), 1),
            "TempMotor": round(38 + index + 3 * math.sin(elapsed / 25 + index), 1),
            "VoltsIn": round(24 + 0.2 * math.sin(elapsed / 10), 2),
            "WhCharged": 0.0,
            "WhUsed": round(24 * elapsed * (0.8 + index * 0.1) / 3600, 6),
            "Timestamp": timestamp,
        }, 0

    yield from sample_imu_messages(elapsed, imu_ids, timestamp)


def main():
    args = parse_args()
    if args.settings:
        print(f"App settings: {args.settings}")
    client = None
    try:
        if not args.dry_run:
            try:
                import paho.mqtt.client as mqtt
            except ImportError:
                raise RuntimeError("Install dependencies: python -m pip install -r scripts/requirements.txt") from None
            connected = threading.Event()
            connection_errors = []

            def on_connect(client, userdata, flags, reason_code, properties):
                if reason_code.is_failure:
                    connection_errors.append(str(reason_code))
                connected.set()

            client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2)
            client.on_connect = on_connect
            if args.username:
                client.username_pw_set(args.username, args.password)
            if args.tls:
                client.tls_set(ca_certs=args.ca_cert or None)
            client.connect_timeout = 10
            client.connect(args.host, args.port, keepalive=30)
            client.loop_start()
            if not connected.wait(10):
                raise RuntimeError("Timed out waiting for MQTT CONNACK")
            if connection_errors:
                raise RuntimeError(f"Broker rejected connection: {connection_errors[0]}")
            print(f"Connected to {args.host}:{args.port}")

        topics = {
            "battery": args.topic_main + "/" + args.battery_topic,
            "wheel": args.topic_main + "/" + args.wheel_topic,
            "imu": args.topic_main + "/" + args.imu_topic,
        }
        print(f"Batteries: {topics['battery']}; steering and manipulator: {topics['wheel']}")
        print(f"IMU boards: {topics['imu']}; CAN IDs: " + ", ".join(f"0x{value:02X}" for value in args.imu_ids if value is not None))
        print("Stop: Ctrl+C")
        started = time.monotonic()
        cycle = 0
        while args.count == 0 or cycle < args.count:
            pending = []
            for kind, payload, qos in sample_messages(time.monotonic() - started, args.steering_ids, args.gripper_range, args.imu_ids):
                topic = topics[kind]
                encoded = json.dumps(payload, separators=(",", ":"), allow_nan=False)
                if args.dry_run:
                    print(f"{topic} {encoded}")
                else:
                    message = client.publish(topic, encoded, qos=qos, retain=False)
                    if message.rc != mqtt.MQTT_ERR_SUCCESS:
                        raise RuntimeError(f"Publish failed on {topic}: {mqtt.error_string(message.rc)}")
                    pending.append(message)
            for message in pending:
                message.wait_for_publish(timeout=10)
                if not message.is_published():
                    raise RuntimeError("Timed out sending telemetry")
            cycle += 1
            if not args.dry_run:
                imu_count = sum(value is not None for value in args.imu_ids)
                print(f"Cycle {cycle}: sent 3 battery, 4 steering, 7 manipulator and {imu_count} IMU messages", flush=True)
            if args.count == 0 or cycle < args.count:
                time.sleep(max(0, started + cycle * args.interval - time.monotonic()))
    except KeyboardInterrupt:
        print("\nStopped.")
    except (OSError, RuntimeError, ValueError) as exc:
        print(f"Error: {exc}", file=sys.stderr)
        return 1
    finally:
        if client is not None:
            client.disconnect()
            client.loop_stop()
    return 0


if __name__ == "__main__":
    sys.exit(main())
