namespace SKAIChips_Verification_Tool.Core
{
    public interface IBus
    {
        #region Connection

        bool Connect();
        void Disconnect();
        bool IsConnected { get; }

        #endregion

        #region IO

        void WriteBytes(byte[] data);
        byte[] ReadBytes(int length);

        #endregion
    }
}
