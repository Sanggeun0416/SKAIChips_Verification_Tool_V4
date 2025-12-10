namespace SKAIChips_Verification_Tool.Core
{
    /// <summary>
    /// Defines the communication bus contract used to connect to external devices.
    /// </summary>
    public interface IBus
    {
        #region Connection

        /// <summary>
        /// Attempts to open the connection to the bus.
        /// </summary>
        /// <returns><c>true</c> if the connection succeeds; otherwise, <c>false</c>.</returns>
        bool Connect();

        /// <summary>
        /// Closes the bus connection and releases resources.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Gets a value indicating whether the bus is currently connected.
        /// </summary>
        bool IsConnected { get; }

        #endregion

        #region IO

        /// <summary>
        /// Writes the provided bytes to the bus.
        /// </summary>
        /// <param name="data">The data buffer to send.</param>
        void WriteBytes(byte[] data);

        /// <summary>
        /// Reads the specified number of bytes from the bus.
        /// </summary>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The data read from the device.</returns>
        byte[] ReadBytes(int length);

        #endregion
    }
}
