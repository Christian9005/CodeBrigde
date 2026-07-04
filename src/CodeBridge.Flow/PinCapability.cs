namespace CodeBridge.Flow;

[Flags]
public enum PinCapability
{
    None = 0,
    DigitalRead = 1 << 0,
    DigitalWrite = 1 << 1,
    AnalogRead = 1 << 2,
    Pwm = 1 << 3,
    ServoRecommended = 1 << 4,
    Interrupt = 1 << 5,
    I2cSda = 1 << 6,
    I2cScl = 1 << 7,
    SpiMosi = 1 << 8,
    SpiMiso = 1 << 9,
    SpiSck = 1 << 10,
    SpiCs = 1 << 11,
    UartTx = 1 << 12,
    UartRx = 1 << 13,
    BootStrap = 1 << 14,
    Reserved = 1 << 15,
    InputOnly = 1 << 16,
    Adc1 = 1 << 17,
    Adc2 = 1 << 18
}
