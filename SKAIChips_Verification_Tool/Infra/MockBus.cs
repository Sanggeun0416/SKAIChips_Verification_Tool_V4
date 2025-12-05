using SKAIChips_Verification_Tool.Core;

namespace SKAIChips_Verification_Tool.Infra
{
    public sealed class MockBus : IBus
    {
        public bool IsConnected { get; private set; }

        public bool Connect()
        {
            IsConnected = true;
            return true;
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        public void Dispose()
        {
            Disconnect();
        }

        public void WriteBytes(byte[] data)
        {

        }

        public byte[] ReadBytes(int length)
        {
            return new byte[length];
        }
    }
}
