using System.Collections.Generic;
using SKAIChips_Verification_Tool.Core;

namespace SKAIChips_Verification_Tool.Chips
{
    public sealed class MockRegisterChip : IRegisterChip
    {
        #region Fields

        private readonly Dictionary<uint, uint> _registers = new();

        #endregion

        #region Properties

        public string Name => "MockChip";

        #endregion

        #region Methods

        public uint ReadRegister(uint address) =>
            _registers.TryGetValue(address, out var value) ? value : 0;

        public void WriteRegister(uint address, uint data)
        {
            _registers[address] = data;
        }

        #endregion
    }
}
