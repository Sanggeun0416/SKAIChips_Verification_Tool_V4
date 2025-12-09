namespace SKAIChips_Verification_Tool.Core
{
    public interface IRegisterChip
    {
        #region Properties

        string Name { get; }

        #endregion

        #region Methods

        uint ReadRegister(uint address);
        void WriteRegister(uint address, uint data);

        #endregion
    }
}
