# Symulator telemetrii MQTT

Uruchom z katalogu repozytorium (Python 3.10+):

```powershell
python -m pip install -r scripts/requirements.txt
python scripts/mqtt_telemetry_simulator.py
```

Na Windows automatycznie wczytuje ustawienia z
`%APPDATA%\Godot\app_userdata\Rover Control App\RoverControlAppSettings.json`.
Na starcie wypisuje uzyty plik oraz pelne tematy MQTT. Parametr `--defaults`
pomija zapisany plik i wybiera fabryczne ustawienia aplikacji.

Bez zapisanego pliku laczy sie z `broker.hivemq.com:1883`. Co 0.05 sekundy (20 Hz)
wysyla trzy wiadomosci na `RappTORS/BatteryInfo`
(sloty 0, 1, 2) i cztery na `RappTORS/VescStatus` (silniki skretne:
FL `0x60`, FR `0x61`, RR `0x62`, RL `0x63`). `PrecisePos` i `PidPos` zawieraja
zmieniajace sie katy w stopniach. Nazwy pol JSON odpowiadaja `Core/MqttClasses.cs`,
enumy sa liczbami, a `Timestamp` to czas Unix w milisekundach.
Baterie maja status Discharging i trzy zamkniete hotswapy (`HotswapStatus = 7`).
Wiadomosci baterii maja QoS 2, silnikow QoS 0; retain jest wylaczony.

Kazdy cykl zawiera dodatkowo siedem ramek manipulatora na tym samym temacie
co silniki skretne (`TopicWheelFeedback`, np. `RappTORS/wheel_feedback`).
Razem z piecioma ramkami IMU opisanymi ponizej daje to domyslnie 19 wiadomosci
na cykl (docelowo 380 wiadomosci/s przy 20 Hz). Identyfikatory sa liczbami w JSON.

| Element | VescId (hex / JSON) | Zakres PrecisePos i PidPos |
| --- | --- | --- |
| Axis1 | 0x70 / 112 | -110 do 110 stopni |
| Axis2 | 0x71 / 113 | -20 do 110 stopni |
| Axis3 | 0x72 / 114 | -70 do 40 stopni |
| Axis4 | 0x73 / 115 | -180 do 180 stopni |
| Axis5 | 0x74 / 116 | -120 do 120 stopni |
| Axis6 | 0x75 / 117 | -180 do 180 stopni |
| Chwytak | 0x76 / 118 | testowo 0 do 1 |

Osie poruszaja sie plynnie miedzy granicami. `PrecisePos` to pozycja aktualna,
a `PidPos` to pozycja zadana, wyprzedzajaca ja o 0.2 s. Ramki zawieraja tez
`ERPM`, `Current`, `CurrentIn`, `DutyCycle`, `TempFet`, `TempMotor`, `VoltsIn`,
`ADC1`-`ADC3`, `PPM`, `Tachometer`, `AhUsed`, `AhCharged`, `WhUsed`, `WhCharged`
i `Timestamp`. To przykladowe dane testowe VESC. Zakres chwytaka nie zostal
okreslony przez protokol aplikacji; mozna go ustawic opcja
`--gripper-range MIN MAX`, np. `--gripper-range 0 90`.
Obecnie animacja chwytaka w `ThreeDManipulator.cs` jest zakomentowana.

Ramki IMUBoard sa wysylane na `TopicImuBoardFeedback` z ustawien MQTT
(fabrycznie pelny temat `RappTORS/ImuBoardFeedback`). Format to plaski JSON:
`VescID` jest liczba calkowita (CAN ID plytki), a `W`, `X`, `Y`, `Z` sa liczbami
zmiennoprzecinkowymi kwaternionu. `Timestamp` to czas Unix w milisekundach.
Nazwy pol odpowiadaja klasie `MqttClasses.ImuBoardFeedback`, z uwzglednieniem
wielkosci liter. Przyklad:

```json
{"VescID":176,"W":1.0,"X":0.0,"Y":0.0,"Z":0.0,"Timestamp":1794268800000}
```

| Plytka | VescID (hex / JSON) |
| --- | --- |
| FrontLeft | 0xB0 / 176 |
| FrontRight | 0xB1 / 177 |
| RearRight | 0xB2 / 178 |
| RearLeft | 0xB3 / 179 |
| Body | 0xB4 / 180 |

ID sa wczytywane z `CanIds.ImuBoards` zapisanego pliku ustawien. Puste `Body`
wylacza wysylanie ramki tej plytki. Mozna nadpisac piec ID opcja
`--imu-ids 0xB0 0xB1 0xB2 0xB3 0xB4` lub zmienic podtemat przez `--imu-topic`.

Kwaterniony sa znormalizowane i zmieniaja sie plynnie dla kazdej plytki.
Symulacja stosuje konwencje body-to-world ZYX, a nastepnie usuwa yaw zgodnie
z funkcja `IMU_WithoutYaw` w
[VescGyro/Core/Src/imu.c](https://github.com/Wojtuz/VescGyro/blob/main/Core/Src/imu.c).
Wysylany jest tylko przechyl i pochylenie: `q_tilt = q_z(-yaw) * q`,
po normalizacji i ograniczeniu skladowych do [-1, 1]. `Z` kwaternionu moze
byc niezerowe przy jednoczesnym roll i pitch, mimo ze kat yaw wynosi zero.
Pola `W`, `X`, `Y`, `Z` sa juz wartosciami float; symulator nie skaluje ich do int16.
IMU korzysta z QoS 0 i retain=false.

Broker, tematy i identyfikatory musza zgadzac sie z ustawieniami uruchomionej
aplikacji. Mozna wczytac jej zapisany plik ustawien:

```powershell
python scripts/mqtt_telemetry_simulator.py --settings "C:\sciezka\RoverControlAppSettings.json"
```

Parametry wiersza polecen nadpisuja ustawienia z pliku:

```powershell
python scripts/mqtt_telemetry_simulator.py --host localhost --port 1883 --topic-main RappTORS --interval 0.1
python scripts/mqtt_telemetry_simulator.py --steering-ids 0x60 0x61 0x62 0x63 --count 10
```

`--battery-topic`, `--wheel-topic` i `--imu-topic` przyjmuja podtematy bez prefiksu `TopicMain`.
Przykladowo, jesli aplikacja ma zapisane `TopicWheelFeedback = "wheel_feedback"`,
symulator wysle dane na `RappTORS/wheel_feedback`, a nie `RappTORS/VescStatus`.
Dostepne sa takze `--username`, `--password`, `--tls` i `--ca-cert`.
Haslo mozna przekazac zmienna srodowiskowa `MQTT_PASSWORD`.
TLS sprawdza certyfikat brokera; w razie potrzeby podaj certyfikat CA.

Podglad jednej serii bez brokera i bez instalowania zaleznosci:

```powershell
python scripts/mqtt_telemetry_simulator.py --dry-run --count 1
```

`--count 0` (domyslnie) wysyla dane do zatrzymania przez Ctrl+C.
`--help` pokazuje wszystkie opcje. Skrypt publikuje telemetrie baterii, skretu, manipulatora i IMU;
tryb baterii `AltMode` w aplikacji korzysta z telemetrii silnikow napedowych,
wiec do testu baterii uzyj zwyklego trybu.
