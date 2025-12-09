using System.Collections.Generic;

namespace SKAIChips_Verification_Tool.Core
{
    public class RegisterGroup
    {
        #region Properties

        public string Name { get; }

        public List<Register> Registers { get; } = new List<Register>();

        #endregion

        #region Constructors

        public RegisterGroup(string name)
        {
            Name = name;
        }

        #endregion

        #region Methods

        public Register AddRegister(string name, uint address)
        {
            var reg = new Register(name, address);
            Registers.Add(reg);
            return reg;
        }

        #endregion
    }
}
