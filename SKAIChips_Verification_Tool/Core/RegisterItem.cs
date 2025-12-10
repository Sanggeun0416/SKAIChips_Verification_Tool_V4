namespace SKAIChips_Verification_Tool.Core
{
    /// <summary>
    /// Represents an individual bit field within a register.
    /// </summary>
    public class RegisterItem
    {
        #region Properties

        /// <summary>
        /// Gets the name of the bit field.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the most significant bit index for the field.
        /// </summary>
        public int UpperBit { get; }

        /// <summary>
        /// Gets the least significant bit index for the field.
        /// </summary>
        public int LowerBit { get; }

        /// <summary>
        /// Gets the default value of the field.
        /// </summary>
        public uint DefaultValue { get; }

        /// <summary>
        /// Gets the description of the field.
        /// </summary>
        public string Description { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterItem"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="upperBit">The most significant bit index.</param>
        /// <param name="lowerBit">The least significant bit index.</param>
        /// <param name="defaultValue">The default value for the field.</param>
        /// <param name="description">The textual description of the field.</param>
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
