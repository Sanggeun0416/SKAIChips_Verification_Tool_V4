using System.Collections.Generic;

namespace SKAIChips_Verification_Tool.Core
{
    /// <summary>
    /// Represents a register entry including its metadata and bit field items.
    /// </summary>
    public class Register
    {
        #region Properties

        /// <summary>
        /// Gets the register name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the register address.
        /// </summary>
        public uint Address { get; }

        /// <summary>
        /// Gets or sets the computed reset value of the register.
        /// </summary>
        public uint ResetValue { get; set; }

        /// <summary>
        /// Gets the collection of bit field items contained in the register.
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
        /// <param name="name">The bit field name.</param>
        /// <param name="upperBit">The most significant bit index.</param>
        /// <param name="lowerBit">The least significant bit index.</param>
        /// <param name="defaultValue">The default value of the field.</param>
        /// <param name="description">A description of the field.</param>
        public void AddItem(string name, int upperBit, int lowerBit, uint defaultValue, string description)
        {
            Items.Add(new RegisterItem(name, upperBit, lowerBit, defaultValue, description));
        }

        #endregion
    }
}
