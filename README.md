# CodeBridge SDK

> **Bridge the gap between full-stack developers and microcontrollers.**

CodeBridge lets you control ESP32, Arduino, STM32, and PIC microcontrollers using C# and .NET — no embedded C/C++ knowledge required.

```csharp
// Connect to an ESP32 and blink an LED — it's that simple
await using var board = await CodeBridgeBuilder
    .Connect()
    .Serial("COM3")
    .ToESP32()
    .BuildAsync();

await board.Gpio.SetPinModeAsync(2, PinMode.Output);
await board.Gpio.DigitalWriteAsync(2, PinValue.High);
```

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                   Your C# App                       │
│          (Console, WinForms, Blazor, MAUI)          │
├─────────────────────────────────────────────────────┤
│              CodeBridge.Core                        │
│    IBoard  │  IGpio  │  II2C  │  ICodeGenerator     │
├─────────────────────────────────────────────────────┤
│           CodeBridge.Transport                      │
│         Serial  │  WiFi  │  BLE  │  MQTT            │
├──────────┬──────────┬───────────┬───────────────────┤
│  ESP32   │ Arduino  │  STM32   │  PIC              │
│(firmware)│(firmware)│(firmware) │(firmware)          │
└──────────┴──────────┴───────────┴───────────────────┘
```

## Project Structure

```
CodeBridge/
├── src/
│   ├── CodeBridge.Core/          # Abstractions & protocol
│   ├── CodeBridge.Transport/     # Serial, WiFi, BLE transports
│   └── CodeBridge.ESP32/         # ESP32 board implementation
├── firmware/
│   └── esp32-bridge/             # PlatformIO firmware for ESP32
├── samples/
│   ├── BlinkLed/                 # "Hello World" of hardware (Serial)
│   └── WiFiBlink/                # Blink LED over WiFi (TCP)
├── tests/
│   └── CodeBridge.Core.Tests/    # Unit tests
└── CodeBridge.slnx               # .NET Solution
```

## Quick Start

### 1. Flash the Firmware

```bash
# Install PlatformIO CLI, then:
cd firmware/esp32-bridge
pio run --target upload
```

### 2. Install the NuGet Package (coming soon)

```bash
dotnet add package CodeBridge.ESP32
```

### 3. Write Your First Program

```csharp
using CodeBridge.Core;
using CodeBridge.Core.Enums;
using CodeBridge.ESP32;

// Connect
await using var board = await CodeBridgeBuilder
    .Connect()
    .Serial("COM3")    // Your ESP32's port
    .ToESP32()
    .BuildAsync();

// Get board info
var info = await board.GetInfoAsync();
Console.WriteLine($"{info.ChipModel} @ {info.CpuFrequencyMHz}MHz");

// Control GPIO
await board.Gpio.SetPinModeAsync(2, PinMode.Output);
await board.Gpio.DigitalWriteAsync(2, PinValue.High);  // LED ON!

// Read analog sensor
await board.Gpio.SetPinModeAsync(34, PinMode.Analog);
int value = await board.Gpio.AnalogReadAsync(34);
Console.WriteLine($"Sensor: {value}");

// Scan I2C devices
var devices = await board.I2C.ScanAsync();
Console.WriteLine($"Found {devices.Count} I2C devices");
```

### 4. Or Connect via WiFi (no cable!)

```csharp
// First, configure WiFi via serial (one-time setup), then:
await using var board = await CodeBridgeBuilder
    .Connect()
    .WiFi("192.168.1.100")   // Your ESP32's IP
    .ToESP32()
    .BuildAsync();

// Same API — just wireless!
await board.Gpio.DigitalWriteAsync(2, PinValue.High);
```

## Hybrid Modes

### Remote Control Mode (MVP)
Your C# app sends commands in real-time to the microcontroller via Serial/WiFi/BLE. Perfect for prototyping, dashboards, and IoT.

### Code Generation Mode (Future)
Define logic in C#, generate standalone C/C++ firmware that runs independently on the micro. Perfect for production deployment.

## Supported Hardware

| Board          | Status       | Transport       |
|----------------|-------------|-----------------|
| ESP32          | ✅ MVP       | Serial, WiFi    |
| ESP32-S3       | 🔜 Next     | Serial, WiFi    |
| Arduino Uno    | 📋 Planned  | Serial          |
| Arduino Mega   | 📋 Planned  | Serial          |
| STM32F4        | 📋 Planned  | Serial, SWD     |
| PIC18F4550     | 📋 Planned  | Serial          |

## Roadmap

### Phase 1: Foundation (Current)
- [x] Core abstractions (IBoard, IGpio, II2C)
- [x] Serial transport
- [x] ESP32 board implementation
- [x] Bridge firmware for ESP32
- [x] Protocol definition
- [x] Unit tests
- [ ] NuGet package publishing

### Phase 2: Expand Communication
- [x] WiFi transport (TCP sockets)
- [ ] MQTT transport
- [ ] BLE transport
- [ ] Board auto-discovery

### Phase 3: More Boards
- [ ] Arduino support (Uno, Mega, Nano)
- [ ] STM32 support
- [ ] PIC18F support
- [ ] SPI controller interface
- [ ] UART controller interface

### Phase 4: Visual Designer (LabVIEW-style)
- [ ] WinForms drag-and-drop block editor
- [ ] Logic blocks (If/Then, Loops, Timers)
- [ ] Sensor blocks (Temperature, Distance, Light)
- [ ] Actuator blocks (Motor, Servo, Relay)
- [ ] Real-time data visualization
- [ ] Export to standalone firmware (Code Generation)

### Phase 5: UI Components
- [ ] Blazor components for IoT dashboards
- [ ] MAUI components for cross-platform apps
- [ ] Real-time charts and gauges
- [ ] Pin configurator UI
- [ ] OTA firmware update from UI

### Phase 6: VS Extension
- [ ] Visual Studio extension with project templates
- [ ] Toolbox with board components
- [ ] Serial monitor integration
- [ ] Pin mapping designer
- [ ] One-click firmware deploy

### Phase 7: Platform Expansion
- [ ] React/Web middleware (REST API + WebSocket)
- [ ] Cloud dashboard (Azure IoT integration)
- [ ] Mobile app support
- [ ] Multi-board orchestration

## Protocol Reference

Commands are sent as text over serial at 115200 baud:

| Command | Format | Description |
|---------|--------|-------------|
| `PM`    | `PM:pin:mode` | Set pin mode (0=IN, 1=OUT, 2=PULLUP, 3=PULLDOWN, 4=ANALOG) |
| `DW`    | `DW:pin:value` | Digital write (0=LOW, 1=HIGH) |
| `DR`    | `DR:pin` | Digital read → `OK:0` or `OK:1` |
| `AR`    | `AR:pin` | Analog read → `OK:value` (0-4095) |
| `PW`    | `PW:pin:duty:freq` | PWM write (duty 0-255) |
| `IS`    | `IS` | I2C scan → `OK:addr1,addr2,...` |
| `PING`  | `PING` | Health check → `OK:PONG` |
| `INFO`  | `INFO` | Board info → `OK:{json}` |
| `VER`   | `VER` | Firmware version → `OK:0.2.0` |
| `WCFG`  | `WCFG:ssid:pass` | Configure WiFi → `OK:ip:port` |
| `WSTAT` | `WSTAT` | WiFi status → `OK:{json}` |
| `WSCAN` | `WSCAN` | Scan WiFi networks → `OK:{json}` |

## Contributing

This project is in early development. Contributions welcome!

## License

MIT License - See [LICENSE](LICENSE) for details.
