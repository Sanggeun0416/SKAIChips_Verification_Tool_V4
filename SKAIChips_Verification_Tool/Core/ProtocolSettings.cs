namespace SKAIChips_Verification_Tool.Core
{
    public class ProtocolSettings
    {
        public ProtocolType ProtocolType { get; set; }

        public int SpeedKbps { get; set; }

        public byte? I2cSlaveAddress { get; set; }

        public int? SpiClockKHz { get; set; }
        public int? SpiMode { get; set; }
    }
}
