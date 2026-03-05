namespace CodeBridge.Core.Protocol;

/// <summary>
/// Defines the command protocol between .NET and the microcontroller firmware.
/// Commands are sent as simple text lines for debugging ease.
/// Format: CMD:PARAM1:PARAM2:...\n
/// Response: OK:DATA or ERR:MESSAGE
/// </summary>
public static class BridgeProtocol
{
    // ── GPIO Commands ────────────────────────────────────────
    public const string CMD_PIN_MODE     = "PM";   // PM:pin:mode
    public const string CMD_DIGITAL_WRITE = "DW";  // DW:pin:value
    public const string CMD_DIGITAL_READ  = "DR";  // DR:pin → OK:value
    public const string CMD_ANALOG_READ   = "AR";  // AR:pin → OK:value
    public const string CMD_PWM_WRITE     = "PW";  // PW:pin:duty:freq

    // ── I2C Commands ─────────────────────────────────────────
    public const string CMD_I2C_SCAN     = "IS";   // IS → OK:addr1,addr2,...
    public const string CMD_I2C_WRITE    = "IW";   // IW:addr:hex_data
    public const string CMD_I2C_READ     = "IR";   // IR:addr:length → OK:hex_data
    public const string CMD_I2C_WREG     = "IWR";  // IWR:addr:reg:hex_data
    public const string CMD_I2C_RREG     = "IRR";  // IRR:addr:reg:length → OK:hex_data

    // ── System Commands ──────────────────────────────────────
    public const string CMD_PING         = "PING"; // PING → OK:PONG
    public const string CMD_INFO         = "INFO"; // INFO → OK:json
    public const string CMD_RESET        = "RST";  // RST → (board resets)
    public const string CMD_VERSION      = "VER";  // VER → OK:version

    // ── WiFi Commands ────────────────────────────────────────
    public const string CMD_WIFI_CONFIG  = "WCFG";  // WCFG:SSID:PASS → OK:IP:PORT
    public const string CMD_WIFI_STATUS  = "WSTAT"; // WSTAT → OK:{json}
    public const string CMD_WIFI_SCAN    = "WSCAN"; // WSCAN → OK:{json}

    // ── SPI Commands ─────────────────────────────────────────
    public const string CMD_SPI_TRANSFER = "ST";   // ST:csPin:hexData → OK:hexData
    public const string CMD_SPI_WRITE    = "SW";   // SW:csPin:hexData → OK
    public const string CMD_SPI_READ     = "SR";   // SR:csPin:length → OK:hexData
    public const string CMD_SPI_CONFIG   = "SC";   // SC:speed:mode → OK

    // ── OneWire Commands ─────────────────────────────────────
    public const string CMD_OW_SCAN      = "OWS";  // OWS:pin → OK:addr1,addr2,...
    public const string CMD_OW_READ      = "OWR";  // OWR:pin:addr:len → OK:hexData
    public const string CMD_OW_WRITE     = "OWW";  // OWW:pin:addr:hexData → OK
    public const string CMD_OW_TEMP      = "OWT";  // OWT:pin[:addr] → OK:tempC

    // ── Servo Commands ───────────────────────────────────────
    public const string CMD_SERVO_ATTACH = "SA";   // SA:pin[:min:max] → OK
    public const string CMD_SERVO_WRITE  = "SV";   // SV:pin:angle → OK
    public const string CMD_SERVO_READ   = "SVR";  // SVR:pin → OK:angle
    public const string CMD_SERVO_US     = "SU";   // SU:pin:microseconds → OK
    public const string CMD_SERVO_DETACH = "SD";   // SD:pin → OK

    // ── NeoPixel (WS2812) Commands ───────────────────────────
    public const string CMD_NEO_INIT     = "NI";   // NI:pin:count → OK
    public const string CMD_NEO_SET      = "NS";   // NS:index:r:g:b → OK
    public const string CMD_NEO_ALL      = "NA";   // NA:r:g:b → OK
    public const string CMD_NEO_SHOW     = "NH";   // NH → OK (push to strip)
    public const string CMD_NEO_CLEAR    = "NC";   // NC → OK
    public const string CMD_NEO_BRIGHT   = "NB";   // NB:brightness → OK
    public const string CMD_NEO_RANGE    = "NR";   // NR:start:r1:g1:b1:r2:g2:b2:... → OK

    // ── Buzzer/Tone Commands ─────────────────────────────────
    public const string CMD_TONE         = "TN";   // TN:pin:freq:durationMs → OK
    public const string CMD_NO_TONE      = "NT";   // NT:pin → OK

    // ── DHT Sensor Commands ──────────────────────────────────
    public const string CMD_DHT_READ     = "DHTR"; // DHTR:pin:type → OK:temp:humidity
    public const string CMD_DHT_INIT     = "DHTI"; // DHTI:pin:type → OK

    // ── Ultrasonic (HC-SR04) Commands ────────────────────────
    public const string CMD_ULTRA_READ   = "USR";  // USR:trigPin:echoPin → OK:distanceCm

    // ── Motor Commands ───────────────────────────────────────
    public const string CMD_MOTOR_INIT   = "MI";   // MI:in1:in2:enPin → OK
    public const string CMD_MOTOR_SPEED  = "MS";   // MS:in1:speed(-100..100) → OK
    public const string CMD_MOTOR_STOP   = "MX";   // MX:in1 → OK
    public const string CMD_STEPPER_INIT = "STI";  // STI:p1:p2:p3:p4:stepsPerRev → OK
    public const string CMD_STEPPER_STEP = "STS";  // STS:p1:steps:speed → OK

    // ── Display Commands ─────────────────────────────────────
    public const string CMD_OLED_INIT    = "OI";   // OI:width:height:addr → OK
    public const string CMD_OLED_CLEAR   = "OC";   // OC → OK
    public const string CMD_OLED_TEXT    = "OT";   // OT:x:y:size:text → OK
    public const string CMD_OLED_PIXEL   = "OP";   // OP:x:y:color → OK
    public const string CMD_OLED_LINE    = "OL";   // OL:x1:y1:x2:y2:color → OK
    public const string CMD_OLED_RECT    = "OR";   // OR:x:y:w:h:color:fill → OK
    public const string CMD_OLED_CIRCLE  = "OE";   // OE:cx:cy:r:color → OK
    public const string CMD_OLED_FLUSH   = "OF";   // OF → OK (display buffer)
    public const string CMD_OLED_BRIGHT  = "OB";   // OB:brightness → OK

    public const string CMD_LCD_INIT     = "LI";   // LI:addr:cols:rows → OK
    public const string CMD_LCD_CLEAR    = "LC";   // LC → OK
    public const string CMD_LCD_TEXT     = "LT";   // LT:row:col:text → OK
    public const string CMD_LCD_BACKLIGHT = "LB";  // LB:0|1 → OK
    public const string CMD_LCD_CURSOR   = "LK";   // LK:row:col → OK

    // ── BME280 Commands ──────────────────────────────────────
    public const string CMD_BME280_INIT  = "BMI";  // BMI:addr → OK
    public const string CMD_BME280_READ  = "BMR";  // BMR → OK:temp,humidity,pressure

    // ── BH1750 Commands ──────────────────────────────────────
    public const string CMD_BH1750_INIT  = "BLI";  // BLI:addr → OK
    public const string CMD_BH1750_READ  = "BLR";  // BLR → OK:lux

    // ── MPU6050 Commands ─────────────────────────────────────
    public const string CMD_MPU6050_INIT = "MPI";  // MPI:addr → OK
    public const string CMD_MPU6050_READ = "MPR";  // MPR → OK:ax,ay,az,gx,gy,gz,temp

    // ── TCS34725 Commands ────────────────────────────────────
    public const string CMD_TCS34725_INIT = "TCI"; // TCI:integrationTime:gain → OK
    public const string CMD_TCS34725_READ = "TCR"; // TCR → OK:r,g,b,clear,colorTemp,lux

    // ── INA219 Commands ──────────────────────────────────────
    public const string CMD_INA219_INIT  = "INI";  // INI:addr → OK
    public const string CMD_INA219_READ  = "INR";  // INR → OK:currentMa,voltageV,powerMw,shuntMv

    // ── GPIO Interrupt Commands ──────────────────────────────
    public const string CMD_INT_ATTACH   = "GINT";  // GINT:pin:edge → OK
    public const string CMD_INT_DETACH   = "GINTD"; // GINTD:pin → OK
    public const string CMD_INT_POLL     = "GINTP"; // GINTP → OK:pin:count:edge,...

    // ── Watchdog Commands ────────────────────────────────────
    public const string CMD_WDT_INIT     = "WDI";  // WDI:timeoutMs → OK
    public const string CMD_WDT_FEED     = "WDF";  // WDF → OK
    public const string CMD_WDT_DISABLE  = "WDD";  // WDD → OK

    // ── Deep Sleep Commands ──────────────────────────────────
    public const string CMD_DEEP_SLEEP   = "DSL";  // DSL:seconds → OK:SLEEPING
    public const string CMD_DEEP_SLEEP_PIN = "DSLP"; // DSLP:pin:level → OK:SLEEPING

    // ── OTA Commands ─────────────────────────────────────────
    public const string CMD_OTA_BEGIN    = "OTAB"; // OTAB:url → OK:OTA_COMPLETE
    public const string CMD_OTA_STATUS   = "OTAS"; // OTAS → OK:status,progress

    // ── MQTT Commands ────────────────────────────────────────
    public const string CMD_MQTT_CONNECT    = "MQC"; // MQC:broker:port:clientId → OK
    public const string CMD_MQTT_PUBLISH    = "MQP"; // MQP:topic:message → OK
    public const string CMD_MQTT_SUBSCRIBE  = "MQS"; // MQS:topic → OK
    public const string CMD_MQTT_UNSUBSCRIBE = "MQU"; // MQU:topic → OK
    public const string CMD_MQTT_READ       = "MQR"; // MQR → OK:topic|payload or OK:NONE
    public const string CMD_MQTT_DISCONNECT = "MQD"; // MQD → OK

    // ── Response Prefixes ────────────────────────────────────
    public const string RESP_OK          = "OK";
    public const string RESP_ERROR       = "ERR";

    public const char SEPARATOR = ':';
    public const string TERMINATOR = "\n";

    /// <summary>
    /// Builds a command string from parts.
    /// </summary>
    public static string BuildCommand(string cmd, params object[] args)
    {
        if (args.Length == 0)
            return cmd + TERMINATOR;

        return cmd + SEPARATOR + string.Join(SEPARATOR, args) + TERMINATOR;
    }

    /// <summary>
    /// Parses a response string into status and data.
    /// </summary>
    public static (bool Success, string Data) ParseResponse(string response)
    {
        response = response.Trim();

        if (response.StartsWith(RESP_OK))
        {
            var data = response.Length > RESP_OK.Length + 1
                ? response[(RESP_OK.Length + 1)..]
                : "";
            return (true, data);
        }

        if (response.StartsWith(RESP_ERROR))
        {
            var message = response.Length > RESP_ERROR.Length + 1
                ? response[(RESP_ERROR.Length + 1)..]
                : "Unknown error";
            return (false, message);
        }

        return (false, $"Invalid response: {response}");
    }
}
