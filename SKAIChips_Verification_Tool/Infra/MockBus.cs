using System;
using SKAIChips_Verification_Tool.Core;

namespace SKAIChips_Verification_Tool.Infra
{
    /// <summary>
    /// Mock implementation of <see cref="IBus"/> for UI testing without hardware.
    /// </summary>
    public sealed class MockBus : IBus, IDisposable
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether the mock bus is considered connected.
        /// </summary>
        public bool IsConnected { get; private set; }

        #endregion

        #region Connection

        /// <summary>
        /// Simulates a successful connection.
        /// </summary>
        /// <returns>Always returns <c>true</c>.</returns>
        public bool Connect()
        {
            IsConnected = true;
            return true;
        }

        /// <summary>
        /// Simulates closing the connection.
        /// </summary>
        public void Disconnect()
        {
            IsConnected = false;
        }

        #endregion

        #region IO

        /// <summary>
        /// Ignores the payload because no actual hardware interaction is required.
        /// </summary>
        /// <param name="data">The data that would be transmitted.</param>
        public void WriteBytes(byte[] data)
        {
        }

        /// <summary>
        /// Returns a zeroed buffer of the requested length for read operations.
        /// </summary>
        /// <param name="length">The number of bytes to return.</param>
        /// <returns>An empty array when <paramref name="length"/> is not positive; otherwise, a zero-filled array.</returns>
        public byte[] ReadBytes(int length)
        {
            if (length <= 0)
                return Array.Empty<byte>();

            return new byte[length];
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Releases the mock bus by marking it as disconnected.
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }

        #endregion
    }
}
