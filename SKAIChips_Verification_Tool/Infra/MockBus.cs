using System;
using SKAIChips_Verification_Tool.Core;

namespace SKAIChips_Verification_Tool.Infra
{
    public sealed class MockBus : IBus, IDisposable
    {
        #region Properties

        public bool IsConnected { get; private set; }

        #endregion

        #region Connection

        public bool Connect()
        {
            IsConnected = true;
            return true;
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        #endregion

        #region IO

        public void WriteBytes(byte[] data)
        {
        }

        public byte[] ReadBytes(int length)
        {
            if (length <= 0)
                return Array.Empty<byte>();

            return new byte[length];
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            Disconnect();
        }

        #endregion
    }
}
