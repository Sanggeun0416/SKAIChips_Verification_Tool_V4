using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using SKAIChips_Verification_Tool.Core.AutoTask;

namespace SKAIChips_Verification_Tool
{
    public partial class AutoTaskEditorForm : Form
    {
        private readonly AutoTaskDefinition _definition;

        public AutoTaskEditorForm(AutoTaskDefinition definition)
        {
            InitializeComponent();

            _definition = definition ?? throw new ArgumentNullException(nameof(definition));

            Text = string.IsNullOrWhiteSpace(_definition.Name)
                ? "AutoTask Editor"
                : $"AutoTask Editor - {_definition.Name}";

            InitGrid();
            LoadFromDefinition();
        }

        private void InitGrid()
        {
            // 타입 콤보 박스 항목
            var typeColumn = dgvBlocks.Columns["colType"] as DataGridViewComboBoxColumn;
            if (typeColumn != null)
            {
                typeColumn.Items.Clear();
                typeColumn.Items.Add("Delay");
                typeColumn.Items.Add("RegWrite");
                typeColumn.Items.Add("RegRead");
            }

            dgvBlocks.AllowUserToAddRows = false;
        }

        private void LoadFromDefinition()
        {
            dgvBlocks.Rows.Clear();

            if (_definition.Blocks == null)
                return;

            foreach (var block in _definition.Blocks)
            {
                int rowIndex = dgvBlocks.Rows.Add();
                var row = dgvBlocks.Rows[rowIndex];

                if (block is DelayBlock d)
                {
                    row.Cells["colType"].Value = "Delay";
                    row.Cells["colDelay"].Value = d.Milliseconds.ToString(CultureInfo.InvariantCulture);
                }
                else if (block is RegWriteBlock w)
                {
                    row.Cells["colType"].Value = "RegWrite";
                    row.Cells["colAddr"].Value = $"0x{w.Address:X8}";
                    row.Cells["colValue"].Value = $"0x{w.Value:X8}";
                }
                else if (block is RegReadBlock r)
                {
                    row.Cells["colType"].Value = "RegRead";
                    row.Cells["colAddr"].Value = $"0x{r.Address:X8}";
                    row.Cells["colValue"].Value = r.ResultKey;
                }

                row.Cells["colTitle"].Value = block.Title;
                row.Tag = block;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            int index = dgvBlocks.Rows.Add();
            var row = dgvBlocks.Rows[index];
            row.Cells["colType"].Value = "Delay";
            row.Cells["colDelay"].Value = "1000";
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (dgvBlocks.SelectedRows.Count == 0)
                return;

            foreach (DataGridViewRow row in dgvBlocks.SelectedRows)
            {
                if (!row.IsNewRow)
                    dgvBlocks.Rows.Remove(row);
            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            if (dgvBlocks.SelectedRows.Count != 1)
                return;

            var row = dgvBlocks.SelectedRows[0];
            int idx = row.Index;
            if (idx <= 0)
                return;

            dgvBlocks.Rows.RemoveAt(idx);
            dgvBlocks.Rows.Insert(idx - 1, row);
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            if (dgvBlocks.SelectedRows.Count != 1)
                return;

            var row = dgvBlocks.SelectedRows[0];
            int idx = row.Index;
            if (idx < 0 || idx >= dgvBlocks.Rows.Count - 1)
                return;

            dgvBlocks.Rows.RemoveAt(idx);
            dgvBlocks.Rows.Insert(idx + 1, row);
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                var blocks = BuildBlocksFromGrid();
                _definition.Blocks.Clear();
                _definition.Blocks.AddRange(blocks);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "AutoTask 저장 중 오류: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private List<AutoBlockBase> BuildBlocksFromGrid()
        {
            var list = new List<AutoBlockBase>();

            foreach (DataGridViewRow row in dgvBlocks.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string type = row.Cells["colType"].Value as string;
                if (string.IsNullOrWhiteSpace(type))
                    continue;

                string title = row.Cells["colTitle"].Value as string;

                switch (type)
                {
                    case "Delay":
                        {
                            int ms = 0;
                            var cellVal = row.Cells["colDelay"].Value;
                            if (cellVal != null &&
                                int.TryParse(cellVal.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                            {
                                ms = v;
                            }

                            var block = new DelayBlock
                            {
                                Title = string.IsNullOrWhiteSpace(title) ? "Delay" : title,
                                Milliseconds = ms
                            };
                            list.Add(block);
                            break;
                        }

                    case "RegWrite":
                        {
                            uint addr = ParseHex(row.Cells["colAddr"].Value);
                            uint value = ParseHex(row.Cells["colValue"].Value);

                            var block = new RegWriteBlock(addr, value);
                            block.Title = string.IsNullOrWhiteSpace(title) ? "RegWrite" : title;

                            list.Add(block);
                            break;
                        }

                    case "RegRead":
                        {
                            uint addr = ParseHex(row.Cells["colAddr"].Value);
                            string key = row.Cells["colValue"].Value as string;
                            if (string.IsNullOrWhiteSpace(key))
                                key = "LastReadValue";

                            var block = new RegReadBlock(addr);
                            block.Title = string.IsNullOrWhiteSpace(title) ? "RegRead" : title;
                            block.ResultKey = key;

                            list.Add(block);
                            break;
                        }
                }
            }

            return list;
        }

        private static uint ParseHex(object cellValue)
        {
            if (cellValue == null)
                return 0;

            string text = cellValue.ToString().Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            if (uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
                return v;

            return 0;
        }
    }
}
