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
        private uint _currentRegValue;

        private const int I2cTimeoutMs = 200;

        // 32bit 비트 패널용 버튼들 (0~31bit)
        private readonly Button[] _bitButtons = new Button[32];
        private bool _isUpdatingBits;

        public RegisterControlForm()
        {
            InitializeComponent();
            InitUi();
        }

        private void InitUi()
        {
            dgvLog.Rows.Clear();

            // Register Description 그리드 초기화
            if (dgvBits != null)
            {
                dgvBits.AutoGenerateColumns = false;
                dgvBits.Columns.Clear();

                var colBit = new DataGridViewTextBoxColumn
                {
                    Name = "colBit",
                    HeaderText = "Bit",
                    ReadOnly = true
                };
                var colName = new DataGridViewTextBoxColumn
                {
                    Name = "colName",
                    HeaderText = "Name",
                    ReadOnly = true
                };
                var colDefault = new DataGridViewTextBoxColumn
                {
                    Name = "colDefault",
                    HeaderText = "Default",
                    ReadOnly = true
                };
                var colCurrent = new DataGridViewTextBoxColumn
                {
                    Name = "colCurrent",
                    HeaderText = "Current",
                    ReadOnly = false
                };
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

            // 레지스터맵 라벨/버튼 초기 상태
            lblMapFileName.Text = "(No file)";
            btnOpenMapPath.Enabled = false;

            // TreeView 우클릭 메뉴
            var ctx = new ContextMenuStrip();
            var mExpand = new ToolStripMenuItem("모두 펼치기");
            var mCollapse = new ToolStripMenuItem("모두 접기");

            mExpand.Click += (s, e) => tvRegs.ExpandAll();
            mCollapse.Click += (s, e) => tvRegs.CollapseAll();

            ctx.Items.Add(mExpand);
            ctx.Items.Add(mCollapse);
            tvRegs.ContextMenuStrip = ctx;

            InitBitButtons();
            UpdateBitButtonsFromValue(_currentRegValue);
            SetBitButtonsEnabledForItem(null);

            // 패널 크기 바뀔 때마다 버튼 폭 재계산
            flowBitsTop.SizeChanged += (s, e) => UpdateBitButtonLayout();
            flowBitsBottom.SizeChanged += (s, e) => UpdateBitButtonLayout();
            groupRegCont.Resize += (s, e) => UpdateBitButtonLayout();   // 선택사항이지만 있으면 더 자연스러움

            // Hex 텍스트에서 포커스 빠질 때 값 반영
            txtRegValueHex.Leave += txtRegValueHex_Leave;

            // WriteAll / ReadAll 버튼 핸들러 연결
            btnWriteAll.Click += btnWriteAll_Click;
            btnReadAll.Click += btnReadAll_Click;

            // 프로젝트 로딩 및 상태 갱신
            LoadProjects();
            UpdateStatusText();

            btnConnect.Text = "Connect";

            // 초기 표시 상태
            lblRegName.Text = "(No Register)";
            lblRegAddrSummary.Text = "Addr: -";
            lblRegResetSummary.Text = "Reset: -";
            txtRegValueHex.Text = "0x00000000";
        }

        #region 32bit 비트 패널 (버튼 토글)

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
                btn.Height = 25;          // 초기 값
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
            UpdateBitButtonLayout();   // ← 이 줄 추가
        }

        private void UpdateBitButtonLayout()
        {
            int cols = 16;

            // 상단 패널
            if (flowBitsTop.ClientSize.Width > 0)
            {
                int panelWidth = flowBitsTop.ClientSize.Width;

                // 좌우 여백/마진을 조금 빼고 버튼 폭 계산
                int btnWidth = (panelWidth - (cols + 1) * 2) / cols;
                if (btnWidth < 16) btnWidth = 16;
                if (btnWidth > 40) btnWidth = 40;   // 너무 넓어지는 것 방지

                int btnHeight = 25;                 // 높이는 고정

                for (int i = 0; i < 16; i++)
                {
                    var btn = _bitButtons[i];
                    if (btn == null) continue;
                    btn.Width = btnWidth;
                    btn.Height = btnHeight;
                }
            }

            // 하단 패널
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

        private void BitPanels_Resize(object sender, EventArgs e)
        {
            // 한 줄에 16개, 좌우 Margin 고려해서 폭 계산
            int topWidth = Math.Max(20,
                (flowBitsTop.ClientSize.Width - flowBitsTop.Padding.Horizontal - (16 * 2)) / 16);
            int bottomWidth = Math.Max(20,
                (flowBitsBottom.ClientSize.Width - flowBitsBottom.Padding.Horizontal - (16 * 2)) / 16);

            for (int i = 0; i < 16; i++)
            {
                if (_bitButtons[i] != null)
                {
                    _bitButtons[i].Width = topWidth;
                    _bitButtons[i].Height = 25; // 사용자가 고정 원함
                }
            }

            for (int i = 16; i < 32; i++)
            {
                if (_bitButtons[i] != null)
                {
                    _bitButtons[i].Width = bottomWidth;
                    _bitButtons[i].Height = 25;
                }
            }
        }

        private void BitButton_Click(object sender, EventArgs e)
        {
            if (_isUpdatingBits)
                return;

            if (sender is not Button btn)
                return;

            // 0/1 토글
            btn.Text = (btn.Text == "0") ? "1" : "0";

            // 전체 32bit 값 다시 계산
            _currentRegValue = GetValueFromBitButtons();

            // 이 값 기준으로 Hex 텍스트 + Register Description 그리드까지 싹 동기화
            UpdateBitCurrentValues();
        }

        private void UpdateBitButtonsFromValue(uint value)
        {
            _isUpdatingBits = true;

            // 화면상 왼쪽 버튼이 bit31, 오른쪽이 bit0 이 되도록 매핑
            for (int bit = 0; bit < 32; bit++)
            {
                int btnIndex = 31 - bit; // bit31 -> index0, bit0 -> index31
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

            // 화면상 index0(맨 왼쪽)이 bit31, index31(맨 오른쪽)이 bit0
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

        #endregion

        #region 공통 유틸/상태

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

            try
            {
                if (!_bus.Connect())
                {
                    _bus = null;
                    MessageBox.Show("FTDI 연결 실패");
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

        #endregion

        #region 버튼 핸들러 (Connect/Read/Write/WriteAll/ReadAll)

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

                UpdateBitCurrentValues(); // Register Description + Hex + 패널 동기화
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

            // 1) 비트필드 한 줄 선택 → 해당 필드만 수정
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
            // 2) 비트 선택 없음 + dgvBits 내용 있으면 → 전체 재조합
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
            // 3) 비트필드도 안 쓰면 → 32bit Hex 텍스트 사용
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

            if (_selectedGroup == null)
            {
                MessageBox.Show("Tree에서 그룹(시트)을 선택하세요.");
                return;
            }

            foreach (var reg in _selectedGroup.Registers)
            {
                uint addr = reg.Address;
                uint data = reg.ResetValue;

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

                    AddLog("WRITE_ALL", $"0x{addr:X8}", $"0x{data:X8}", "OK");
                }
                catch (Exception ex)
                {
                    AddLog("WRITE_ALL", $"0x{addr:X8}", $"0x{data:X8}", "ERR");
                    Debug.WriteLine(ex);
                }
            }
        }

        private async void btnReadAll_Click(object sender, EventArgs e)
        {
            if (_chip == null)
            {
                MessageBox.Show("먼저 Connect 하세요.");
                return;
            }

            if (_selectedGroup == null)
            {
                MessageBox.Show("Tree에서 그룹(시트)을 선택하세요.");
                return;
            }

            foreach (var reg in _selectedGroup.Registers)
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
                    AddLog("READ_ALL", $"0x{addr:X8}", $"0x{data:X8}", "OK");
                }
                catch (Exception ex)
                {
                    AddLog("READ_ALL", $"0x{addr:X8}", "", "ERR");
                    Debug.WriteLine(ex);
                }
            }
        }

        #endregion

        #region 레지스터맵 로딩 / TreeView

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

        private void tvRegs_AfterSelect(object sender, TreeViewEventArgs e)
        {
            _selectedGroup = null;
            _selectedRegister = null;

            if (e.Node?.Tag is RegisterGroup g)
            {
                _selectedGroup = g;
                dgvBits.Rows.Clear();
                lblRegName.Text = "(Group Selected)";
                lblRegAddrSummary.Text = "Addr: -";
                lblRegResetSummary.Text = "Reset: -";
                _currentRegValue = 0;
                UpdateBitCurrentValues();

                // 그룹 선택 시: 모든 비트 활성화
                SetBitButtonsEnabledForItem(null);
                return;
            }

            if (e.Node?.Tag is Register reg)
            {
                if (e.Node.Parent?.Tag is RegisterGroup pg)
                    _selectedGroup = pg;

                _selectedRegister = reg;
                lblRegName.Text = reg.Name;
                lblRegAddrSummary.Text = $"Addr: 0x{reg.Address:X8}";
                lblRegResetSummary.Text = $"Reset: 0x{reg.ResetValue:X8}";

                if (_selectedGroup != null)
                {
                    int idx = _selectedGroup.Registers.IndexOf(reg);
                    if (idx >= 0 && idx <= (int)numRegIndex.Maximum)
                        numRegIndex.Value = idx;
                }

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

                _currentRegValue = reg.ResetValue;
                UpdateBitCurrentValues();

                // 레지스터만 선택된 상태에서는 32비트 전체 활성
                SetBitButtonsEnabledForItem(null);
            }
            else if (e.Node?.Tag is RegisterItem item)
            {
                if (e.Node.Parent?.Tag is Register parentReg)
                {
                    if (e.Node.Parent.Parent?.Tag is RegisterGroup pg)
                        _selectedGroup = pg;

                    _selectedRegister = parentReg;
                    lblRegName.Text = parentReg.Name;
                    lblRegAddrSummary.Text = $"Addr: 0x{parentReg.Address:X8}";
                    lblRegResetSummary.Text = $"Reset: 0x{parentReg.ResetValue:X8}";

                    if (_selectedGroup != null)
                    {
                        int idx = _selectedGroup.Registers.IndexOf(parentReg);
                        if (idx >= 0 && idx <= (int)numRegIndex.Maximum)
                            numRegIndex.Value = idx;
                    }

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

                    _currentRegValue = parentReg.ResetValue;
                    UpdateBitCurrentValues();

                    // 여기서 선택된 필드 범위만 버튼 활성화
                    SetBitButtonsEnabledForItem(item);
                }
            }
            else
            {
                dgvBits.Rows.Clear();
                lblRegName.Text = "(No Register)";
                lblRegAddrSummary.Text = "Addr: -";
                lblRegResetSummary.Text = "Reset: -";
                _currentRegValue = 0;
                UpdateBitCurrentValues();

                // 아무것도 선택 안 된 상태 → 전체 활성
                SetBitButtonsEnabledForItem(null);
            }
        }

        private void SetBitButtonsEnabledForItem(RegisterItem item)
        {
            if (_bitButtons == null)
                return;

            // 선택된 비트/필드가 없는 경우 → 전부 활성화
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

            // item.LowerBit ~ item.UpperBit 범위만 활성화
            for (int bit = 0; bit < 32; bit++)
            {
                int btnIndex = 31 - bit;          // 화면 왼쪽이 bit31, 오른쪽이 bit0
                var btn = _bitButtons[btnIndex];
                if (btn == null) continue;

                bool inRange = (bit >= item.LowerBit) && (bit <= item.UpperBit);
                btn.Enabled = inRange;
            }
        }

        private void UpdateBitCurrentValues()
        {
            // 1) Register Description 그리드의 colCurrent 갱신
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

            // 2) 32bit Hex 텍스트 갱신
            txtRegValueHex.Text = $"0x{_currentRegValue:X8}";

            // 3) 32bit 버튼 패널 갱신
            UpdateBitButtonsFromValue(_currentRegValue);
        }

        #endregion

        #region 프로젝트/프로토콜/FTDI 설정

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

        #endregion
    }
}
