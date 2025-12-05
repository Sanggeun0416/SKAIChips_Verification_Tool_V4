using ClosedXML.Excel;
using SKAIChips_Verification_Tool.Chips;
using SKAIChips_Verification_Tool.Core;
using SKAIChips_Verification_Tool.Infra;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SKAIChips_Verification_Tool
{
    public partial class RegisterControlForm : Form
    {
        private IBus _bus;
        private IRegisterChip _chip;

        private readonly List<IChipProject> _projects = new();
        private IChipProject _selectedProject;

        private FtdiDeviceSettings _ftdiSettings;
        private ProtocolSettings _protocolSettings;

        private string _regMapFilePath;
        private readonly List<RegisterGroup> _groups = new();
        private RegisterGroup _selectedGroup;
        private Register _selectedRegister;
        private RegisterItem _selectedItem;
        private uint _currentRegValue;
        private bool _isUpdatingRegValue;
        private string _scriptFilePath;

        private const int I2cTimeoutMs = 200;

        private readonly Button[] _bitButtons = new Button[32];
        private bool _isUpdatingBits;

        private readonly Dictionary<Register, uint> _regValues = new();

        public RegisterControlForm()
        {
            InitializeComponent();
            InitUi();
        }

        private void InitUi()
        {
            dgvLog.Rows.Clear();

            if (dgvBits != null)
            {
                dgvBits.AutoGenerateColumns = false;
                dgvBits.Columns.Clear();

                var colBit = new DataGridViewTextBoxColumn { Name = "colBit", HeaderText = "Bit", ReadOnly = true };
                var colName = new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Name", ReadOnly = true };
                var colDefault = new DataGridViewTextBoxColumn { Name = "colDefault", HeaderText = "Default", ReadOnly = true };
                var colCurrent = new DataGridViewTextBoxColumn { Name = "colCurrent", HeaderText = "Current", ReadOnly = false };
                var colDesc = new DataGridViewTextBoxColumn
                {
                    Name = "colDesc",
                    HeaderText = "Description",
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                };

                dgvBits.Columns.Add(colBit);
                dgvBits.Columns.Add(colName);
                dgvBits.Columns.Add(colDefault);
                dgvBits.Columns.Add(colCurrent);
                dgvBits.Columns.Add(colDesc);
            }

            lblMapFileName.Text = "(No file)";
            btnOpenMapPath.Enabled = false;

            var ctx = new ContextMenuStrip();
            var mExpand = new ToolStripMenuItem("모두 펼치기");
            var mCollapse = new ToolStripMenuItem("모두 접기");
            var mSearch = new ToolStripMenuItem("검색...");

            mExpand.Click += (s, e) => tvRegs.ExpandAll();
            mCollapse.Click += (s, e) => tvRegs.CollapseAll();
            mSearch.Click += (s, e) => ShowTreeSearchDialog();

            ctx.Items.Add(mExpand);
            ctx.Items.Add(mCollapse);
            ctx.Items.Add(new ToolStripSeparator());
            ctx.Items.Add(mSearch);
            tvRegs.ContextMenuStrip = ctx;

            InitBitButtons();
            UpdateBitButtonsFromValue(_currentRegValue);
            SetBitButtonsEnabledForItem(null);

            flowBitsTop.SizeChanged += (s, e) => UpdateBitButtonLayout();
            flowBitsBottom.SizeChanged += (s, e) => UpdateBitButtonLayout();
            groupRegCont.Resize += (s, e) => UpdateBitButtonLayout();

            txtRegValueHex.Leave += txtRegValueHex_Leave;

            btnWriteAll.Click += btnWriteAll_Click;
            btnReadAll.Click += btnReadAll_Click;

            LoadProjects();
            UpdateStatusText();

            btnConnect.Text = "Connect";

            lblRegName.Text = "(No Register)";
            lblRegAddrSummary.Text = "Address: -";
            lblRegResetSummary.Text = "Reset Value: -";
            txtRegValueHex.Text = "0x00000000";

            numRegIndex.Minimum = 0;
            numRegIndex.Maximum = 0;
            numRegIndex.Value = 0;
            numRegIndex.Enabled = false;
            numRegIndex.ValueChanged += numRegIndex_ValueChanged;

            lblScriptFileName.Text = "(No script)";
            btnOpenScriptPath.Enabled = false;
        }

        private void InitBitButtons()
        {
            flowBitsTop.Controls.Clear();
            flowBitsBottom.Controls.Clear();

            for (int i = 0; i < 32; i++)
            {
                var btn = new Button();
                btn.Margin = new Padding(1);
                btn.Padding = new Padding(0);
                btn.Width = 24;
                btn.Height = 25;
                btn.Text = "0";
                btn.Tag = i;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 1;
                btn.Click += BitButton_Click;

                _bitButtons[i] = btn;

                if (i < 16)
                    flowBitsTop.Controls.Add(btn);
                else
                    flowBitsBottom.Controls.Add(btn);
            }

            UpdateBitButtonsFromValue(_currentRegValue);
            UpdateBitButtonLayout();
        }

        private string PromptText(string title, string label, string defaultValue)
        {
            using (var form = new Form())
            using (var lbl = new Label())
            using (var txt = new TextBox())
            using (var btnOk = new Button())
            using (var btnCancel = new Button())
            {
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ClientSize = new Size(320, 120);

                lbl.AutoSize = true;
                lbl.Text = label;
                lbl.Location = new Point(9, 9);

                txt.Size = new Size(300, 23);
                txt.Location = new Point(9, 30);
                txt.Text = defaultValue;

                btnOk.Text = "OK";
                btnOk.DialogResult = DialogResult.OK;
                btnOk.Location = new Point(152, 70);

                btnCancel.Text = "Cancel";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.Location = new Point(234, 70);

                form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                var result = form.ShowDialog(this);
                if (result == DialogResult.OK)
                    return txt.Text;

                return null;
            }
        }

        private void ShowTreeSearchDialog()
        {
            string text = PromptText("Register 검색", "검색할 텍스트를 입력하세요:", "");
            if (string.IsNullOrWhiteSpace(text))
                return;

            var node = FindTreeNodeContains(tvRegs.Nodes, text);
            if (node == null)
            {
                MessageBox.Show("일치하는 항목이 없습니다.");
                return;
            }

            tvRegs.SelectedNode = node;
            tvRegs.Focus();
            node.EnsureVisible();
        }

        private TreeNode FindTreeNodeContains(TreeNodeCollection nodes, string text)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    return node;

                var child = FindTreeNodeContains(node.Nodes, text);
                if (child != null)
                    return child;
            }
            return null;
        }

        private void UpdateBitButtonLayout()
        {
            int cols = 16;

            if (flowBitsTop.ClientSize.Width > 0)
            {
                int panelWidth = flowBitsTop.ClientSize.Width;

                int btnWidth = (panelWidth - (cols + 1) * 2) / cols;
                if (btnWidth < 16) btnWidth = 16;
                if (btnWidth > 40) btnWidth = 40;

                int btnHeight = 25;

                for (int i = 0; i < 16; i++)
                {
                    var btn = _bitButtons[i];
                    if (btn == null) continue;
                    btn.Width = btnWidth;
                    btn.Height = btnHeight;
                }
            }

            if (flowBitsBottom.ClientSize.Width > 0)
            {
                int panelWidth = flowBitsBottom.ClientSize.Width;

                int btnWidth = (panelWidth - (cols + 1) * 2) / cols;
                if (btnWidth < 16) btnWidth = 16;
                if (btnWidth > 40) btnWidth = 40;

                int btnHeight = 25;

                for (int i = 16; i < 32; i++)
                {
                    var btn = _bitButtons[i];
                    if (btn == null) continue;
                    btn.Width = btnWidth;
                    btn.Height = btnHeight;
                }
            }
        }

        private void BitButton_Click(object sender, EventArgs e)
        {
            if (_isUpdatingBits)
                return;

            if (sender is not Button btn)
                return;

            btn.Text = (btn.Text == "0") ? "1" : "0";

            _currentRegValue = GetValueFromBitButtons();
            UpdateBitCurrentValues();
        }

        private void UpdateBitButtonsFromValue(uint value)
        {
            _isUpdatingBits = true;

            for (int bit = 0; bit < 32; bit++)
            {
                int btnIndex = 31 - bit;
                uint mask = 1u << bit;
                bool isOne = (value & mask) != 0;

                var btn = _bitButtons[btnIndex];
                if (btn != null)
                    btn.Text = isOne ? "1" : "0";
            }

            _isUpdatingBits = false;
        }

        private uint GetValueFromBitButtons()
        {
            uint value = 0;

            for (int btnIndex = 0; btnIndex < 32; btnIndex++)
            {
                var btn = _bitButtons[btnIndex];
                if (btn == null) continue;

                int bit = 31 - btnIndex;
                if (btn.Text == "1")
                    value |= (1u << bit);
            }

            return value;
        }

        private void txtRegValueHex_Leave(object sender, EventArgs e)
        {
            if (TryParseHexUInt(txtRegValueHex.Text, out uint v))
            {
                _currentRegValue = v;
                UpdateBitCurrentValues();
            }
            else
            {
                MessageBox.Show("레지스터 값 형식이 잘못되었습니다. 예: 0x00000000");
                txtRegValueHex.Text = $"0x{_currentRegValue:X8}";
            }
        }

        private void LoadProjects()
        {
            _projects.Clear();
            comboProject.Items.Clear();

            var projectType = typeof(IChipProject);
            var asm = typeof(OasisProject).Assembly;

            foreach (var t in asm.GetTypes())
            {
                if (t.IsAbstract || t.IsInterface)
                    continue;

                if (!projectType.IsAssignableFrom(t))
                    continue;

                if (Activator.CreateInstance(t) is IChipProject proj)
                {
                    _projects.Add(proj);
                    comboProject.Items.Add(proj.Name);
                }
            }

            if (comboProject.Items.Count > 0)
                comboProject.SelectedIndex = 0;
        }

        private void UpdateStatusText()
        {
            if (_protocolSettings == null)
            {
                lblProtocolInfo.Text = "(Not set)";
            }
            else
            {
                string t = _protocolSettings.ProtocolType.ToString();

                if (_protocolSettings.ProtocolType == ProtocolType.I2C)
                {
                    t += $" / {_protocolSettings.SpeedKbps} kHz";

                    if (_protocolSettings.I2cSlaveAddress.HasValue)
                        t += $" / 0x{_protocolSettings.I2cSlaveAddress.Value:X2}";
                }

                lblProtocolInfo.Text = t;
            }

            if (_ftdiSettings == null)
            {
                lblFtdiInfo.Text = "(Not set)";
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(_ftdiSettings.Description))
                    lblFtdiInfo.Text = _ftdiSettings.Description;
                else
                    lblFtdiInfo.Text = $"DevIdx {_ftdiSettings.DeviceIndex}";
            }

            bool isConnected = _bus != null && _bus.IsConnected;

            lblStatus.Text = isConnected ? "Connected" : "Disconnected";
            lblStatus.ForeColor = isConnected ? Color.LimeGreen : Color.DarkRed;
        }

        private async Task<(bool success, T result)> RunWithTimeout<T>(Func<T> action, int timeoutMs)
        {
            var task = Task.Run(action);
            var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
            if (completed == task)
                return (true, task.Result);
            return (false, default!);
        }

        private async Task<bool> RunWithTimeout(Action action, int timeoutMs)
        {
            var task = Task.Run(action);
            var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
            return completed == task;
        }

        private bool TryParseHexUInt(string text, out uint value)
        {
            text = text.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            return uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        private bool TryParseFieldValue(object cellValue, int width, out uint value)
        {
            value = 0;

            if (cellValue == null)
                return false;

            string text = cellValue.ToString().Trim();

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            if (!uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                return false;

            uint max = width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);
            if (value > max)
                value = max;

            return true;
        }

        private void AddLog(string type, string addrText, string dataText, string result)
        {
            int rowIndex = dgvLog.Rows.Add();
            var row = dgvLog.Rows[rowIndex];

            row.Cells["colTime"].Value = DateTime.Now.ToString("HH:mm:ss");
            row.Cells["colType"].Value = type;
            row.Cells["colAddr"].Value = addrText;
            row.Cells["colData"].Value = dataText;
            row.Cells["colResult"].Value = result;

            dgvLog.FirstDisplayedScrollingRowIndex = rowIndex;
        }

        private void DisconnectBus()
        {
            try
            {
                _bus?.Disconnect();
            }
            catch
            {
            }

            _bus = null;
            _chip = null;

            btnConnect.Text = "Connect";
            UpdateStatusText();
        }

        private void TryConnect()
        {
            if (_selectedProject == null)
            {
                MessageBox.Show("프로젝트를 선택하세요.");
                return;
            }

            bool isMockProject = _selectedProject is MockProject;

            if (!isMockProject)
            {
                if (_ftdiSettings == null)
                {
                    MessageBox.Show("FTDI 장비 셋업이 필요합니다.");
                    return;
                }

                if (_protocolSettings == null)
                {
                    MessageBox.Show("프로토콜 셋업이 필요합니다.");
                    return;
                }

                if (_protocolSettings.ProtocolType == ProtocolType.I2C)
                {
                    if (!_protocolSettings.I2cSlaveAddress.HasValue)
                    {
                        MessageBox.Show("I2C Slave Address가 설정되지 않았습니다.");
                        return;
                    }

                    uint devIndex = (uint)_ftdiSettings.DeviceIndex;
                    ushort slaveAddr = (ushort)_protocolSettings.I2cSlaveAddress.Value;
                    ushort speedKbps = (ushort)_protocolSettings.SpeedKbps;

                    _bus = new Ft4222I2cBus(devIndex, slaveAddr, speedKbps);
                }
                else
                {
                    MessageBox.Show("해당 프로토콜은 아직 구현되지 않았습니다.");
                    return;
                }
            }
            else
            {
                _bus = new MockBus();
            }

            try
            {
                if (!_bus.Connect())
                {
                    _bus = null;
                    MessageBox.Show(isMockProject ? "Mock 연결 실패" : "FTDI 연결 실패");
                    UpdateStatusText();
                    return;
                }

                _chip = _selectedProject.CreateChip(_bus, _protocolSettings);

                btnConnect.Text = "Disconnect";
                UpdateStatusText();
            }
            catch (Exception ex)
            {
                DisconnectBus();
                MessageBox.Show("연결 중 오류: " + ex.Message);
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (_bus == null || !_bus.IsConnected)
                TryConnect();
            else
                DisconnectBus();
        }

        private async void btnRead_Click(object sender, EventArgs e)
        {
            if (_chip == null)
            {
                MessageBox.Show("먼저 Connect 하세요.");
                return;
            }

            if (_selectedRegister == null)
            {
                MessageBox.Show("먼저 레지스터를 선택하세요.");
                return;
            }

            uint addr = _selectedRegister.Address;

            try
            {
                var result = await RunWithTimeout(() => _chip.ReadRegister(addr), I2cTimeoutMs);

                if (!result.success)
                {
                    AddLog("READ", $"0x{addr:X8}", "", "TIMEOUT");
                    return;
                }

                uint data = result.result;
                _currentRegValue = data;

                AddLog("READ", $"0x{addr:X8}", $"0x{data:X8}", "OK");

                UpdateBitCurrentValues();
            }
            catch (Exception ex)
            {
                AddLog("READ", $"0x{addr:X8}", "", "ERR");
                MessageBox.Show(ex.Message, "Read Error");
            }
        }

        private async void btnWrite_Click(object sender, EventArgs e)
        {
            if (_chip == null)
            {
                MessageBox.Show("먼저 Connect 하세요.");
                return;
            }

            if (_selectedRegister == null)
            {
                MessageBox.Show("먼저 레지스터를 선택하세요.");
                return;
            }

            uint addr = _selectedRegister.Address;
            uint newValue;

            if (dgvBits.SelectedRows.Count > 0 && dgvBits.SelectedRows[0].Tag is RegisterItem selItem)
            {
                var row = dgvBits.SelectedRows[0];
                int width = selItem.UpperBit - selItem.LowerBit + 1;

                if (!TryParseFieldValue(row.Cells["colCurrent"].Value, width, out uint fieldVal))
                {
                    MessageBox.Show("Current 값 형식이 잘못되었습니다. 예: 0x1");
                    return;
                }

                uint baseValue = _currentRegValue;

                uint mask = (width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u)) << selItem.LowerBit;
                newValue = baseValue;
                newValue &= ~mask;
                newValue |= (fieldVal << selItem.LowerBit);
            }
            else if (_selectedRegister != null && dgvBits.Rows.Count > 0)
            {
                uint baseValue = _currentRegValue;
                newValue = baseValue;

                foreach (DataGridViewRow r in dgvBits.Rows)
                {
                    if (r.Tag is not RegisterItem item)
                        continue;

                    int width = item.UpperBit - item.LowerBit + 1;
                    var cell = r.Cells["colCurrent"].Value;

                    if (cell == null || string.IsNullOrWhiteSpace(cell.ToString()))
                        continue;

                    if (!TryParseFieldValue(cell, width, out uint fieldVal))
                        continue;

                    uint mask = (width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u)) << item.LowerBit;
                    newValue &= ~mask;
                    newValue |= (fieldVal << item.LowerBit);
                }
            }
            else
            {
                if (!TryParseHexUInt(txtRegValueHex.Text, out newValue))
                {
                    MessageBox.Show("레지스터 값 형식이 잘못되었습니다. 예: 0x00000001");
                    return;
                }
            }

            try
            {
                bool success = await RunWithTimeout(() =>
                {
                    _chip.WriteRegister(addr, newValue);
                }, I2cTimeoutMs);

                if (!success)
                {
                    AddLog("WRITE", $"0x{addr:X8}", $"0x{newValue:X8}", "TIMEOUT");
                    return;
                }

                _currentRegValue = newValue;
                AddLog("WRITE", $"0x{addr:X8}", $"0x{newValue:X8}", "OK");
                UpdateBitCurrentValues();
            }
            catch (Exception ex)
            {
                AddLog("WRITE", $"0x{addr:X8}", $"0x{newValue:X8}", "ERR");
                MessageBox.Show(ex.Message, "Write Error");
            }
        }

        private async void btnWriteAll_Click(object sender, EventArgs e)
        {
            if (_chip == null)
            {
                MessageBox.Show("먼저 Connect 하세요.");
                return;
            }

            if (_groups == null || _groups.Count == 0)
            {
                MessageBox.Show("먼저 Register Tree를 로드하세요.");
                return;
            }

            foreach (var group in _groups)
            {
                foreach (var reg in group.Registers)
                {
                    uint addr = reg.Address;
                    uint data = GetRegisterValue(reg);

                    try
                    {
                        bool success = await RunWithTimeout(() =>
                        {
                            _chip.WriteRegister(addr, data);
                        }, I2cTimeoutMs);

                        if (!success)
                        {
                            AddLog("WRITE_ALL", $"0x{addr:X8}", $"0x{data:X8}", "TIMEOUT");
                            continue;
                        }

                        _regValues[reg] = data;
                        AddLog("WRITE_ALL", $"0x{addr:X8}", $"0x{data:X8}", "OK");
                    }
                    catch (Exception ex)
                    {
                        AddLog("WRITE_ALL", $"0x{addr:X8}", $"0x{data:X8}", "ERR");
                        Debug.WriteLine(ex);
                    }
                }
            }

            if (_selectedRegister != null)
            {
                _currentRegValue = GetRegisterValue(_selectedRegister);
                UpdateBitCurrentValues();
            }
        }

        private async void btnReadAll_Click(object sender, EventArgs e)
        {
            if (_chip == null)
            {
                MessageBox.Show("먼저 Connect 하세요.");
                return;
            }

            if (_groups == null || _groups.Count == 0)
            {
                MessageBox.Show("먼저 Register Tree를 로드하세요.");
                return;
            }

            foreach (var group in _groups)
            {
                foreach (var reg in group.Registers)
                {
                    uint addr = reg.Address;

                    try
                    {
                        var result = await RunWithTimeout(() => _chip.ReadRegister(addr), I2cTimeoutMs);

                        if (!result.success)
                        {
                            AddLog("READ_ALL", $"0x{addr:X8}", "", "TIMEOUT");
                            continue;
                        }

                        uint data = result.result;
                        _regValues[reg] = data;
                        AddLog("READ_ALL", $"0x{addr:X8}", $"0x{data:X8}", "OK");
                    }
                    catch (Exception ex)
                    {
                        AddLog("READ_ALL", $"0x{addr:X8}", "", "ERR");
                        Debug.WriteLine(ex);
                    }
                }
            }

            if (_selectedRegister != null)
            {
                _currentRegValue = GetRegisterValue(_selectedRegister);
                UpdateBitCurrentValues();
            }
        }

        private void btnSelectMapFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Excel Files|*.xlsx;*.xlsm;*.xls";
                ofd.Title = "Select RegisterMap Excel";

                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    _regMapFilePath = ofd.FileName;
                    lblMapFileName.Text = Path.GetFileName(_regMapFilePath);
                    btnOpenMapPath.Enabled = true;
                    clbSheets.Items.Clear();

                    using (var wb = new XLWorkbook(_regMapFilePath))
                    {
                        foreach (var ws in wb.Worksheets)
                            clbSheets.Items.Add(ws.Name);
                    }

                    _groups.Clear();
                    _regValues.Clear();
                    tvRegs.Nodes.Clear();
                    dgvBits.Rows.Clear();
                }
                catch (Exception ex)
                {
                    _regMapFilePath = null;
                    lblMapFileName.Text = "(No file)";
                    btnOpenMapPath.Enabled = false;
                    MessageBox.Show("엑셀 파일 열기 실패: " + ex.Message);
                }
            }
        }

        private void btnLoadSelectedSheets_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_regMapFilePath))
            {
                MessageBox.Show("먼저 엑셀 파일을 선택하세요.");
                return;
            }

            if (clbSheets.CheckedItems.Count == 0)
            {
                MessageBox.Show("로드할 시트를 선택하세요.");
                return;
            }

            try
            {
                _groups.Clear();
                _regValues.Clear();

                using (var wb = new XLWorkbook(_regMapFilePath))
                {
                    foreach (var item in clbSheets.CheckedItems)
                    {
                        string sheetName = item.ToString();
                        var ws = wb.Worksheet(sheetName);
                        string[,] data = ExcelHelper.WorksheetToArray(ws);
                        RegisterGroup group = RegisterMapParser.MakeRegisterGroup(sheetName, data);
                        _groups.Add(group);
                    }
                }

                BuildRegisterTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show("RegisterMap 로딩 실패: " + ex.Message);
            }
        }

        private void btnOpenMapPath_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_regMapFilePath) || !File.Exists(_regMapFilePath))
            {
                MessageBox.Show("열려 있는 레지스터맵 파일이 없습니다.");
                return;
            }

            try
            {
                var arg = $"/select,\"{_regMapFilePath}\"";
                Process.Start("explorer.exe", arg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("경로 오픈 실패: " + ex.Message);
            }
        }

        private void BuildRegisterTree()
        {
            tvRegs.Nodes.Clear();

            foreach (var g in _groups)
            {
                var groupNode = new TreeNode(g.Name)
                {
                    Tag = g
                };

                foreach (var reg in g.Registers)
                {
                    var regNode = new TreeNode($"{reg.Name} (0x{reg.Address:X8})")
                    {
                        Tag = reg
                    };

                    foreach (var item in reg.Items)
                    {
                        string bitText = item.UpperBit == item.LowerBit
                            ? item.UpperBit.ToString()
                            : $"{item.UpperBit}:{item.LowerBit}";

                        var itemNode = new TreeNode($"[{bitText}] {item.Name}")
                        {
                            Tag = item
                        };

                        regNode.Nodes.Add(itemNode);
                    }

                    groupNode.Nodes.Add(regNode);
                }

                tvRegs.Nodes.Add(groupNode);
            }

            tvRegs.BeginUpdate();

            foreach (TreeNode sheetNode in tvRegs.Nodes)
            {
                sheetNode.Expand();

                foreach (TreeNode child in sheetNode.Nodes)
                    child.Collapse();
            }

            tvRegs.EndUpdate();
        }

        private uint GetRegisterValue(Register reg)
        {
            if (reg == null)
                return 0;

            if (_regValues.TryGetValue(reg, out var v))
                return v;

            v = reg.ResetValue;
            _regValues[reg] = v;
            return v;
        }

        private void tvRegs_AfterSelect(object sender, TreeViewEventArgs e)
        {
            _selectedGroup = null;
            _selectedRegister = null;
            _selectedItem = null;

            if (e.Node?.Tag is RegisterGroup g)
            {
                _selectedGroup = g;

                dgvBits.Rows.Clear();
                lblRegName.Text = "(Group Selected)";
                lblRegAddrSummary.Text = "Address: -";
                lblRegResetSummary.Text = "Reset Value: -";
                _currentRegValue = 0;
                UpdateBitCurrentValues();

                SetBitButtonsEnabledForItem(null);
                UpdateNumRegIndexForSelectedItem();
                return;
            }

            if (e.Node?.Tag is Register reg)
            {
                if (e.Node.Parent?.Tag is RegisterGroup pg)
                    _selectedGroup = pg;

                _selectedRegister = reg;
                _selectedItem = null;

                lblRegName.Text = reg.Name;
                lblRegAddrSummary.Text = $"Address: 0x{reg.Address:X8}";
                lblRegResetSummary.Text = $"Reset Value: 0x{reg.ResetValue:X8}";

                dgvBits.Rows.Clear();

                foreach (var item in reg.Items)
                {
                    int rowIndex = dgvBits.Rows.Add();
                    var row = dgvBits.Rows[rowIndex];

                    string bitText = item.UpperBit == item.LowerBit
                        ? item.UpperBit.ToString()
                        : $"{item.UpperBit}:{item.LowerBit}";

                    row.Cells["colBit"].Value = bitText;
                    row.Cells["colName"].Value = item.Name;
                    row.Cells["colDefault"].Value = $"0x{item.DefaultValue:X}";
                    row.Cells["colCurrent"].Value = "";
                    row.Cells["colDesc"].Value = item.Description;

                    row.Tag = item;
                }

                _currentRegValue = GetRegisterValue(reg);
                UpdateBitCurrentValues();

                SetBitButtonsEnabledForItem(null);
                UpdateNumRegIndexForSelectedItem();
            }
            else if (e.Node?.Tag is RegisterItem item)
            {
                if (e.Node.Parent?.Tag is Register parentReg)
                {
                    if (e.Node.Parent.Parent?.Tag is RegisterGroup pg)
                        _selectedGroup = pg;

                    _selectedRegister = parentReg;
                    _selectedItem = item;

                    lblRegName.Text = parentReg.Name;
                    lblRegAddrSummary.Text = $"Address: 0x{parentReg.Address:X8}";
                    lblRegResetSummary.Text = $"Reset Value: 0x{parentReg.ResetValue:X8}";

                    dgvBits.Rows.Clear();

                    foreach (var it in parentReg.Items)
                    {
                        int rowIndex = dgvBits.Rows.Add();
                        var row = dgvBits.Rows[rowIndex];

                        string bitText = it.UpperBit == it.LowerBit
                            ? it.UpperBit.ToString()
                            : $"{it.UpperBit}:{it.LowerBit}";

                        row.Cells["colBit"].Value = bitText;
                        row.Cells["colName"].Value = it.Name;
                        row.Cells["colDefault"].Value = $"0x{it.DefaultValue:X}";
                        row.Cells["colCurrent"].Value = "";
                        row.Cells["colDesc"].Value = it.Description;

                        row.Tag = it;

                        if (ReferenceEquals(it, item))
                            row.Selected = true;
                    }

                    _currentRegValue = GetRegisterValue(parentReg);
                    UpdateBitCurrentValues();

                    SetBitButtonsEnabledForItem(item);
                    UpdateNumRegIndexForSelectedItem();
                }
            }
            else
            {
                _selectedGroup = null;
                _selectedRegister = null;
                _selectedItem = null;

                dgvBits.Rows.Clear();
                lblRegName.Text = "(No Register)";
                lblRegAddrSummary.Text = "Address: -";
                lblRegResetSummary.Text = "Reset Value: -";
                _currentRegValue = 0;
                UpdateBitCurrentValues();

                SetBitButtonsEnabledForItem(null);
                UpdateNumRegIndexForSelectedItem();
            }
        }

        private void SetBitButtonsEnabledForItem(RegisterItem item)
        {
            if (_bitButtons == null)
                return;

            if (item == null)
            {
                for (int i = 0; i < _bitButtons.Length; i++)
                {
                    var btn = _bitButtons[i];
                    if (btn != null)
                        btn.Enabled = true;
                }
                return;
            }

            for (int bit = 0; bit < 32; bit++)
            {
                int btnIndex = 31 - bit;
                var btn = _bitButtons[btnIndex];
                if (btn == null) continue;

                bool inRange = (bit >= item.LowerBit) && (bit <= item.UpperBit);
                btn.Enabled = inRange;
            }
        }

        private void UpdateBitCurrentValues()
        {
            for (int i = 0; i < dgvBits.Rows.Count; i++)
            {
                var row = dgvBits.Rows[i];
                if (row.Tag is not RegisterItem item)
                    continue;

                int width = item.UpperBit - item.LowerBit + 1;
                uint mask = width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);
                uint fieldVal = (_currentRegValue >> item.LowerBit) & mask;

                row.Cells["colCurrent"].Value = $"0x{fieldVal:X}";
            }

            txtRegValueHex.Text = $"0x{_currentRegValue:X8}";

            if (_selectedRegister != null)
                _regValues[_selectedRegister] = _currentRegValue;

            UpdateBitButtonsFromValue(_currentRegValue);

            UpdateNumRegIndexForSelectedItem();
        }

        private void UpdateNumRegIndexForSelectedItem()
        {
            _isUpdatingRegValue = true;
            try
            {
                if (_selectedItem == null)
                {
                    numRegIndex.Enabled = false;
                    numRegIndex.Minimum = 0;
                    numRegIndex.Maximum = 0;
                    numRegIndex.Value = 0;
                    return;
                }

                int width = _selectedItem.UpperBit - _selectedItem.LowerBit + 1;
                uint mask = width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);
                uint fieldVal = (_currentRegValue >> _selectedItem.LowerBit) & mask;

                numRegIndex.Minimum = 0;
                numRegIndex.Maximum = mask;
                numRegIndex.Enabled = true;

                if (fieldVal <= mask)
                    numRegIndex.Value = fieldVal;
                else
                    numRegIndex.Value = mask;
            }
            finally
            {
                _isUpdatingRegValue = false;
            }
        }

        private void numRegIndex_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingRegValue)
                return;

            if (_selectedItem == null)
                return;

            uint fieldVal = (uint)numRegIndex.Value;

            int width = _selectedItem.UpperBit - _selectedItem.LowerBit + 1;
            uint mask = width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);

            if (fieldVal > mask)
                fieldVal = mask;

            uint regVal = _currentRegValue;
            uint fieldMask = mask << _selectedItem.LowerBit;

            regVal &= ~fieldMask;
            regVal |= (fieldVal << _selectedItem.LowerBit);

            _currentRegValue = regVal;
            UpdateBitCurrentValues();
        }

        private void SaveRegisterScriptLegacy(string path)
        {
            using (var sw = new StreamWriter(path))
            {
                foreach (var group in _groups)
                {
                    sw.WriteLine(group.Name);

                    foreach (var reg in group.Registers)
                    {
                        uint value = GetRegisterValue(reg);

                        sw.WriteLine($"\t{reg.Address:X8}\t{value:X8}\t{reg.Name}");

                        foreach (var item in reg.Items)
                        {
                            int width = item.UpperBit - item.LowerBit + 1;
                            uint mask = width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);
                            uint fieldVal = (value >> item.LowerBit) & mask;

                            string bitText = $"[{item.UpperBit}:{item.LowerBit}]";

                            sw.WriteLine($"\t\t{bitText}{item.Name}\t{fieldVal}");
                        }
                    }
                }
            }
        }

        private void LoadRegisterScriptLegacy(string path)
        {
            var addrToReg = new Dictionary<uint, Register>();
            foreach (var g in _groups)
            {
                foreach (var reg in g.Registers)
                    addrToReg[reg.Address] = reg;
            }

            foreach (var raw in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                string line = raw.Trim();

                if (line.StartsWith("["))
                    continue;

                var parts = line.Split(
                    new[] { '\t', ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2 &&
                    TryParseHexUInt(parts[0], out uint addr) &&
                    TryParseHexUInt(parts[1], out uint value))
                {
                    if (addrToReg.TryGetValue(addr, out var reg))
                    {
                        _regValues[reg] = value;
                    }

                    continue;
                }
            }

            if (_selectedRegister != null)
            {
                _currentRegValue = GetRegisterValue(_selectedRegister);
                UpdateBitCurrentValues();
            }
        }

        private void btnSaveScript_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Register Script|*.txt|All Files|*.*";

                if (!string.IsNullOrEmpty(_scriptFilePath))
                {
                    sfd.InitialDirectory = Path.GetDirectoryName(_scriptFilePath);
                    sfd.FileName = Path.GetFileName(_scriptFilePath);
                }

                if (sfd.ShowDialog(this) != DialogResult.OK)
                    return;

                SaveRegisterScriptLegacy(sfd.FileName);

                SetScriptFilePath(sfd.FileName);
            }
        }

        private void btnLoadScript_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Register Script|*.txt|All Files|*.*";

                if (!string.IsNullOrEmpty(_scriptFilePath))
                {
                    ofd.InitialDirectory = Path.GetDirectoryName(_scriptFilePath);
                    ofd.FileName = Path.GetFileName(_scriptFilePath);
                }

                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                LoadRegisterScriptLegacy(ofd.FileName);

                SetScriptFilePath(ofd.FileName);

                if (_selectedRegister != null)
                {
                    _currentRegValue = GetRegisterValue(_selectedRegister);
                    UpdateBitCurrentValues();
                }
            }
        }

        private void SetScriptFilePath(string path)
        {
            _scriptFilePath = path;

            if (string.IsNullOrEmpty(path))
            {
                lblScriptFileName.Text = "(No script)";
                btnOpenScriptPath.Enabled = false;
            }
            else
            {
                lblScriptFileName.Text = Path.GetFileName(path);
                btnOpenScriptPath.Enabled = true;
            }
        }

        private void btnOpenScriptPath_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_scriptFilePath) || !File.Exists(_scriptFilePath))
            {
                MessageBox.Show("열려 있는 스크립트 파일이 없습니다.");
                return;
            }

            var arg = $"/select,\"{_scriptFilePath}\"";
            Process.Start("explorer.exe", arg);
        }

        private void comboProject_SelectedIndexChanged(object sender, EventArgs e)
        {
            var name = comboProject.SelectedItem as string;
            _selectedProject = null;

            foreach (var p in _projects)
            {
                if (p.Name == name)
                {
                    _selectedProject = p;
                    break;
                }
            }

            _protocolSettings = null;
            UpdateStatusText();
        }

        private void btnFtdiSetup_Click(object sender, EventArgs e)
        {
            using (var dlg = new FtdiSetupForm(_ftdiSettings))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _ftdiSettings = dlg.Result;
                    UpdateStatusText();
                }
            }
        }

        private void btnProtocolSetup_Click(object sender, EventArgs e)
        {
            if (_selectedProject == null)
            {
                MessageBox.Show("먼저 프로젝트를 선택하세요.");
                return;
            }

            using (var dlg = new ProtocolSetupForm(_selectedProject, _protocolSettings))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _protocolSettings = dlg.Result;
                    UpdateStatusText();
                }
            }
        }
    }
}
