namespace SKAIChips_Verification_Tool.Core
{
    public interface IBus
    {
        bool Connect();
        void Disconnect();
        bool IsConnected { get; }

        void WriteBytes(byte[] data);
        byte[] ReadBytes(int length);
    }
}
