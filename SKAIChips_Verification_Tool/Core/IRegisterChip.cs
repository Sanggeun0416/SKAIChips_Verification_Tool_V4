namespace SKAIChips_Verification_Tool.Core
{
    /// <summary>
    /// Defines register access operations for a specific chip implementation.
    /// </summary>
    public interface IRegisterChip
    {
        #region Properties

        /// <summary>
        /// Gets the name of the chip.
        /// </summary>
        string Name { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Reads a 32-bit register value from the given address.
        /// </summary>
        /// <param name="address">The register address to read.</param>
        /// <returns>The value read from the register.</returns>
        uint ReadRegister(uint address);

        /// <summary>
        /// Writes a 32-bit value to the specified register address.
        /// </summary>
        /// <param name="address">The register address to write to.</param>
        /// <param name="data">The data to write.</param>
        void WriteRegister(uint address, uint data);

        #endregion
    }
}
