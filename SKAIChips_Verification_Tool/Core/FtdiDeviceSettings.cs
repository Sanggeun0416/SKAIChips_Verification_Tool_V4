namespace SKAIChips_Verification_Tool.Core
{
    public class FtdiDeviceSettings
    {
        #region Properties

        public int DeviceIndex { get; set; }

        public string Description { get; set; }

        public string SerialNumber { get; set; }

        public string Location { get; set; }

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"{DeviceIndex} - {Description} ({SerialNumber})";
        }

        #endregion
    }
}
