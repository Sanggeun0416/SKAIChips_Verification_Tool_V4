using System.Collections.Generic;

namespace SKAIChips_Verification_Tool.Core
{
    /// <summary>
    /// Represents a logical group of registers loaded from a register map.
    /// </summary>
    public class RegisterGroup
    {
        #region Properties

        /// <summary>
        /// Gets the name of the register group.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the collection of registers belonging to the group.
        /// </summary>
        public List<Register> Registers { get; } = new List<Register>();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterGroup"/> class with the specified name.
        /// </summary>
        /// <param name="name">The display name of the group.</param>
        public RegisterGroup(string name)
        {
            Name = name;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new register and adds it to the group.
        /// </summary>
        /// <param name="name">The register name.</param>
        /// <param name="address">The register address.</param>
        /// <returns>The newly created <see cref="Register"/> instance.</returns>
        public Register AddRegister(string name, uint address)
        {
            var reg = new Register(name, address);
            Registers.Add(reg);
            return reg;
        }

        #endregion
    }
}
