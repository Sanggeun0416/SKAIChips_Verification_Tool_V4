using System.Collections.Generic;
using SKAIChips_Verification_Tool.Core;

namespace SKAIChips_Verification_Tool.Chips
{
    public sealed class MockRegisterChip : IRegisterChip
    {
        private readonly Dictionary<uint, uint> _registers = new();

        public string Name => "MockChip";

        public uint ReadRegister(uint address)
        {
            if (!_registers.TryGetValue(address, out var value))
                value = 0;

            return value;
        }

        public void WriteRegister(uint address, uint data)
        {
            _registers[address] = data;
        }
    }
}
