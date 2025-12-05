using SKAIChips_Verification_Tool.Core;

namespace SKAIChips_Verification_Tool.Chips
{
    public interface IChipProject
    {
        string Name { get; }

        ProtocolType[] SupportedProtocols { get; }

        IRegisterChip CreateChip(IBus bus, ProtocolSettings settings);
    }
}
