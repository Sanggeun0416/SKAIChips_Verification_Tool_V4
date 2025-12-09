namespace SKAIChips_Verification_Tool.Core
{
    public class RegisterItem
    {
        #region Properties

        public string Name { get; }
        public int UpperBit { get; }
        public int LowerBit { get; }
        public uint DefaultValue { get; }
        public string Description { get; }

        #endregion

        #region Constructors

        public RegisterItem(string name, int upperBit, int lowerBit, uint defaultValue, string description)
        {
            Name = name;
            UpperBit = upperBit;
            LowerBit = lowerBit;
            DefaultValue = defaultValue;
            Description = description;
        }

        #endregion
    }
}
