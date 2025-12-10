using System.Collections.Generic;

namespace SKAIChips_Verification_Tool.Core
{
    /// <summary>
    /// Represents a register with address information and associated bit field items.
    /// </summary>
    public class Register
    {
        #region Properties

        /// <summary>
        /// Gets the name of the register.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the register address.
        /// </summary>
        public uint Address { get; }

        /// <summary>
        /// Gets or sets the reset value for the register.
        /// </summary>
        public uint ResetValue { get; set; }

        /// <summary>
        /// Gets the collection of register items (bit fields).
        /// </summary>
        public List<RegisterItem> Items { get; } = new List<RegisterItem>();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Register"/> class.
        /// </summary>
        /// <param name="name">The register name.</param>
        /// <param name="address">The register address.</param>
        public Register(string name, uint address)
        {
            Name = name;
            Address = address;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a new bit field item to the register.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="upperBit">The upper bit index of the field.</param>
        /// <param name="lowerBit">The lower bit index of the field.</param>
        /// <param name="defaultValue">The default value of the field.</param>
        /// <param name="description">A description of the field.</param>
        public void AddItem(string name, int upperBit, int lowerBit, uint defaultValue, string description)
        {
            Items.Add(new RegisterItem(name, upperBit, lowerBit, defaultValue, description));
        }

        #endregion
    }
}
