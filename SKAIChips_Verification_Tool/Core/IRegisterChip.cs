namespace SKAIChips_Verification_Tool.Core
{
    /// <summary>
    /// Represents a chip that exposes readable and writable registers.
    /// </summary>
    public interface IRegisterChip
    {
        #region Properties

        /// <summary>
        /// Gets the display name of the chip.
        /// </summary>
        string Name { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Reads a 32-bit value from the specified register address.
        /// </summary>
        /// <param name="address">The address of the register to read.</param>
        /// <returns>The register value.</returns>
        uint ReadRegister(uint address);

        /// <summary>
        /// Writes a 32-bit value to the specified register address.
        /// </summary>
        /// <param name="address">The address of the register to write.</param>
        /// <param name="data">The value to write.</param>
        void WriteRegister(uint address, uint data);

        #endregion
    }
}
