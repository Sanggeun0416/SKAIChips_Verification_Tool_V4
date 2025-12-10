namespace SKAIChips_Verification_Tool.Core
{
    /// <summary>
    /// Represents configuration parameters for the selected communication protocol.
    /// </summary>
    public class ProtocolSettings
    {
        #region Properties

        /// <summary>
        /// Gets or sets the protocol type used by the bus.
        /// </summary>
        public ProtocolType ProtocolType { get; set; }

        /// <summary>
        /// Gets or sets the data rate in kilobits per second.
        /// </summary>
        public int SpeedKbps { get; set; }

        /// <summary>
        /// Gets or sets the optional I2C slave address.
        /// </summary>
        public byte? I2cSlaveAddress { get; set; }

        /// <summary>
        /// Gets or sets the optional SPI clock frequency in kilohertz.
        /// </summary>
        public int? SpiClockKHz { get; set; }

        /// <summary>
        /// Gets or sets the optional SPI mode value.
        /// </summary>
        public int? SpiMode { get; set; }

        #endregion
    }
}
