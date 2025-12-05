using SKAIChips_Verification_Tool.Core;

namespace SKAIChips_Verification_Tool.Chips
{
    public sealed class MockProject : IChipProject
    {
        public string Name => "[Mock] No Device";

        public ProtocolType[] SupportedProtocols { get; } = { ProtocolType.I2C };

        public IRegisterChip CreateChip(IBus bus, ProtocolSettings protocolSettings)
        {
            return new MockRegisterChip();
        }
    }
}
