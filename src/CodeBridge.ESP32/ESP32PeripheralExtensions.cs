using CodeBridge.Core.Abstractions;

namespace CodeBridge.ESP32;

/// <summary>
/// Convenience factory methods for creating ESP32 peripheral drivers from a board/transport.
/// Usage:
///   var servo = board.CreateServo(13);
///   var neo = board.CreateNeoPixel(16, 30);
///   var buzzer = board.CreateBuzzer(25);
///   var dht = board.CreateDhtSensor(4);
///   var ultrasonic = board.CreateUltrasonicSensor(5, 18);
///   var motor = board.CreateMotor(27, 26, 14);
///   var stepper = board.CreateStepper(19, 18, 5, 17);
///   var relay = board.CreateRelay(4);
///   var oled = board.CreateOled();
///   var lcd = board.CreateLcd();
/// </summary>
public static class ESP32PeripheralExtensions
{
    /// <summary>
    /// Creates a servo driver on the specified pin.
    /// </summary>
    public static ESP32Servo CreateServo(this ESP32Board board, int pin, int minPulseUs = 500, int maxPulseUs = 2500)
    {
        return new ESP32Servo(board.Transport, pin, minPulseUs, maxPulseUs);
    }

    /// <summary>
    /// Creates a NeoPixel (WS2812B) LED strip driver.
    /// </summary>
    public static ESP32NeoPixel CreateNeoPixel(this ESP32Board board, int pin, int ledCount)
    {
        return new ESP32NeoPixel(board.Transport, pin, ledCount);
    }

    /// <summary>
    /// Creates a buzzer driver on the specified pin.
    /// </summary>
    public static ESP32Buzzer CreateBuzzer(this ESP32Board board, int pin)
    {
        return new ESP32Buzzer(board.Transport, pin);
    }

    /// <summary>
    /// Creates a DHT temperature/humidity sensor driver.
    /// </summary>
    /// <param name="board">The connected ESP32 board.</param>
    /// <param name="pin">GPIO data pin connected to the DHT sensor.</param>
    /// <param name="dhtType">11 for DHT11, 22 for DHT22/AM2302 (default: 22).</param>
    public static ESP32DhtSensor CreateDhtSensor(this ESP32Board board, int pin, int dhtType = 22)
    {
        return new ESP32DhtSensor(board.Transport, pin, dhtType);
    }

    /// <summary>
    /// Creates an HC-SR04 ultrasonic distance sensor driver.
    /// </summary>
    public static ESP32UltrasonicSensor CreateUltrasonicSensor(this ESP32Board board, int trigPin, int echoPin)
    {
        return new ESP32UltrasonicSensor(board.Transport, trigPin, echoPin);
    }

    /// <summary>
    /// Creates a DC motor driver via H-Bridge (L298N, L293D).
    /// </summary>
    /// <param name="board">The connected ESP32 board.</param>
    /// <param name="in1Pin">First direction pin (IN1).</param>
    /// <param name="in2Pin">Second direction pin (IN2).</param>
    /// <param name="enablePin">PWM enable pin for speed control (-1 if not used).</param>
    public static ESP32MotorController CreateMotor(this ESP32Board board, int in1Pin, int in2Pin, int enablePin = -1)
    {
        return new ESP32MotorController(board.Transport, in1Pin, in2Pin, enablePin);
    }

    /// <summary>
    /// Creates a stepper motor driver (4-wire, half-step).
    /// </summary>
    public static ESP32StepperMotor CreateStepper(this ESP32Board board, int pin1, int pin2, int pin3, int pin4, int stepsPerRevolution = 2048)
    {
        return new ESP32StepperMotor(board.Transport, pin1, pin2, pin3, pin4, stepsPerRevolution);
    }

    /// <summary>
    /// Creates a relay driver on the specified pin.
    /// </summary>
    /// <param name="board">The connected ESP32 board.</param>
    /// <param name="pin">GPIO pin controlling the relay.</param>
    /// <param name="activeLow">True if the relay is active-LOW (most modules are).</param>
    public static ESP32Relay CreateRelay(this ESP32Board board, int pin, bool activeLow = true)
    {
        return new ESP32Relay(board.Transport, pin, activeLow);
    }

    /// <summary>
    /// Creates an SSD1306 OLED display driver (I2C).
    /// </summary>
    public static ESP32OledDisplay CreateOled(this ESP32Board board, int width = 128, int height = 64, int address = 0x3C)
    {
        return new ESP32OledDisplay(board.Transport, width, height, address);
    }

    /// <summary>
    /// Creates an I2C LCD display driver (HD44780 + PCF8574 backpack).
    /// </summary>
    public static ESP32LcdDisplay CreateLcd(this ESP32Board board, int columns = 16, int rows = 2, int address = 0x27)
    {
        return new ESP32LcdDisplay(board.Transport, columns, rows, address);
    }

    // ── Phase 5: I2C Sensors ────────────────────────────────

    /// <summary>Creates a BME280 environmental sensor driver (I2C).</summary>
    public static ESP32Bme280Sensor CreateBme280(this ESP32Board board, int address = 0x76)
        => new ESP32Bme280Sensor(board.Transport, address);

    /// <summary>Creates a BH1750 ambient light sensor driver (I2C).</summary>
    public static ESP32Bh1750Sensor CreateBh1750(this ESP32Board board, int address = 0x23)
        => new ESP32Bh1750Sensor(board.Transport, address);

    /// <summary>Creates an MPU6050 IMU sensor driver (I2C).</summary>
    public static ESP32Mpu6050Sensor CreateMpu6050(this ESP32Board board, int address = 0x68)
        => new ESP32Mpu6050Sensor(board.Transport, address);

    // ── Phase 5: OneWire / GPIO Sensors ─────────────────────

    /// <summary>Creates a DS18B20 OneWire temperature sensor driver.</summary>
    public static ESP32Ds18b20Sensor CreateDs18b20(this ESP32Board board, int pin)
        => new ESP32Ds18b20Sensor(board.Transport, pin);

    /// <summary>Creates a PIR HC-SR501 motion sensor driver (GPIO).</summary>
    public static ESP32PirSensor CreatePir(this ESP32Board board, int pin)
        => new ESP32PirSensor(board.Transport, pin);

    // ── Phase 6: Remaining Sensors & Actuators ──────────────

    /// <summary>Creates an RGB LED driver using 3 PWM pins.</summary>
    /// <param name="board">The connected ESP32 board.</param>
    /// <param name="redPin">GPIO pin for Red channel.</param>
    /// <param name="greenPin">GPIO pin for Green channel.</param>
    /// <param name="bluePin">GPIO pin for Blue channel.</param>
    /// <param name="commonAnode">True for common-anode LEDs (inverts PWM).</param>
    public static ESP32RgbLed CreateRgbLed(this ESP32Board board, int redPin, int greenPin, int bluePin, bool commonAnode = false)
        => new ESP32RgbLed(board.Transport, redPin, greenPin, bluePin, commonAnode);

    /// <summary>Creates an analog gas sensor driver (MQ-2, MQ-135, etc.).</summary>
    /// <param name="board">The connected ESP32 board.</param>
    /// <param name="pin">ADC-capable GPIO pin (32-39).</param>
    /// <param name="gasType">Type of gas detected (e.g., "Smoke", "CO2").</param>
    public static ESP32GasSensor CreateGasSensor(this ESP32Board board, int pin, string gasType = "Smoke")
        => new ESP32GasSensor(board.Transport, pin, gasType);

    /// <summary>Creates a TCS34725 RGB color sensor driver (I2C).</summary>
    /// <param name="board">The connected ESP32 board.</param>
    /// <param name="integrationTimeMs">Integration time in ms: 24, 50, 101, 154, 600.</param>
    /// <param name="gain">Gain multiplier: 1, 4, 16, 60.</param>
    public static ESP32Tcs34725Sensor CreateColorSensor(this ESP32Board board, int integrationTimeMs = 50, int gain = 4)
        => new ESP32Tcs34725Sensor(board.Transport, integrationTimeMs, gain);

    /// <summary>Creates an INA219 current/voltage/power sensor driver (I2C).</summary>
    /// <param name="board">The connected ESP32 board.</param>
    /// <param name="address">I2C address (default: 0x40).</param>
    public static ESP32Ina219Sensor CreateCurrentSensor(this ESP32Board board, int address = 0x40)
        => new ESP32Ina219Sensor(board.Transport, address);
}
