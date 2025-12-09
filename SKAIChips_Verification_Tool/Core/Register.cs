using System.Collections.Generic;

namespace SKAIChips_Verification_Tool.Core
{
    public class Register
    {
        #region Properties

        public string Name { get; }
        public uint Address { get; }
        public uint ResetValue { get; set; }
        public List<RegisterItem> Items { get; } = new List<RegisterItem>();

        #endregion

        #region Constructors

        public Register(string name, uint address)
        {
            Name = name;
            Address = address;
        }

        #endregion

        #region Methods

        public void AddItem(string name, int upperBit, int lowerBit, uint defaultValue, string description)
        {
            Items.Add(new RegisterItem(name, upperBit, lowerBit, defaultValue, description));
        }

        #endregion
    }
}
