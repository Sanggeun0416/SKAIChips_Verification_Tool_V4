namespace SKAIChips_Verification_Tool.Core
{
    /// <summary>
    /// Represents a communication bus used to connect to and exchange data with a device.
    /// </summary>
    public interface IBus
    {
        #region Connection

        /// <summary>
        /// Opens a connection to the target device.
        /// </summary>
        /// <returns><c>true</c> if the connection succeeds; otherwise, <c>false</c>.</returns>
        bool Connect();

        /// <summary>
        /// Closes the current connection to the target device.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Gets a value indicating whether the bus is currently connected and usable.
        /// </summary>
        bool IsConnected { get; }

        #endregion

        #region IO

        /// <summary>
        /// Writes the specified payload to the connected device.
        /// </summary>
        /// <param name="data">The byte sequence to transmit.</param>
        void WriteBytes(byte[] data);

        /// <summary>
        /// Reads a specific number of bytes from the connected device.
        /// </summary>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The received bytes; an empty array when <paramref name="length"/> is not positive.</returns>
        byte[] ReadBytes(int length);

        #endregion
    }
}
