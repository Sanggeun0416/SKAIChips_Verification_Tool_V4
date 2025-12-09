using System;
using System.Globalization;
using System.Windows.Forms;
using SKAIChips_Verification_Tool.Chips;
using SKAIChips_Verification_Tool.Core;

namespace SKAIChips_Verification_Tool
{
    public partial class ProtocolSetupForm : Form
    {
        private readonly IChipProject _project;

        public ProtocolSettings Result { get; private set; }

        public ProtocolSetupForm(IChipProject project, ProtocolSettings current = null)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));

            InitializeComponent();
            InitProtocolCombo();

            if (current != null)
                ApplyCurrent(current);
            else
                UpdateI2cControlsEnabled();
        }

        #region Init / Apply

        private void InitProtocolCombo()
        {
            comboProtocol.Items.Clear();

            foreach (var p in _project.SupportedProtocols)
                comboProtocol.Items.Add(p);

            if (comboProtocol.Items.Count > 0)
                comboProtocol.SelectedIndex = 0;
        }

        private void ApplyCurrent(ProtocolSettings current)
        {
            for (var i = 0; i < comboProtocol.Items.Count; i++)
            {
                if (comboProtocol.Items[i] is ProtocolType pt && pt == current.ProtocolType)
                {
                    comboProtocol.SelectedIndex = i;
                    break;
                }
            }

            if (current.ProtocolType == ProtocolType.I2C)
            {
                if (current.SpeedKbps > 0)
                    numSpeed.Value = current.SpeedKbps;

                if (current.I2cSlaveAddress.HasValue)
                    txtSlaveAddr.Text = $"0x{current.I2cSlaveAddress.Value:X2}";
            }

            UpdateI2cControlsEnabled();
        }

        #endregion

        #region Event Handlers

        private void comboProtocol_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateI2cControlsEnabled();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (comboProtocol.SelectedItem is not ProtocolType protocol)
            {
                MessageBox.Show("Protocol을 선택하세요.");
                return;
            }

            var settings = new ProtocolSettings
            {
                ProtocolType = protocol
            };

            if (protocol == ProtocolType.I2C)
            {
                settings.SpeedKbps = (int)numSpeed.Value;

                if (!TryParseHexByte(txtSlaveAddr.Text, out var slave))
                {
                    MessageBox.Show("I2C Slave Address 형식이 잘못되었습니다. 예: 0x52");
                    return;
                }

                settings.I2cSlaveAddress = slave;
            }

            Result = settings;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        #endregion

        #region Helpers

        private void UpdateI2cControlsEnabled()
        {
            var selected = comboProtocol.SelectedItem;
            var isI2c = selected is ProtocolType pt && pt == ProtocolType.I2C;

            lblSpeed.Enabled = isI2c;
            numSpeed.Enabled = isI2c;
            lblSlaveAddr.Enabled = isI2c;
            txtSlaveAddr.Enabled = isI2c;
        }

        private static bool TryParseHexByte(string text, out byte value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text[2..];

            return byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        #endregion
    }
}
