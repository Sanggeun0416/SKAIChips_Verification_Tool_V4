using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SKAIChips_Verification_Tool.Core;

namespace SKAIChips_Verification_Tool.Chips
{
    public class OasisProject : IChipProject, IChipProjectWithTests
    {
        public string Name => "Oasis";

        public IEnumerable<ProtocolType> SupportedProtocols { get; } = new[] { ProtocolType.I2C };

        public IRegisterChip CreateChip(IBus bus, ProtocolSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            if (settings.ProtocolType != ProtocolType.I2C)
                throw new InvalidOperationException("Oasis supports only I2C.");

            return new OasisRegisterChip("Oasis", bus);
        }

        public IChipTestSuite CreateTestSuite(IRegisterChip chip)
        {
            if (chip is not OasisRegisterChip oasisChip)
                throw new ArgumentException("Chip instance must be OasisRegisterChip.", nameof(chip));

            return new OasisTestSuite(oasisChip);
        }
    }

    internal class OasisRegisterChip : IRegisterChip
    {
        private readonly IBus _bus;

        public string Name { get; }

        public OasisRegisterChip(string name, IBus bus)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public uint ReadRegister(uint address)
        {
            var cmd = BuildAddressCommand(address);
            _bus.WriteBytes(cmd);

            var rcv = _bus.ReadBytes(4);
            var data = (uint)((rcv[3] << 24) | (rcv[2] << 16) | (rcv[1] << 8) | rcv[0]);

            return data;
        }

        public void WriteRegister(uint address, uint data)
        {
            var cmd = BuildAddressCommand(address);
            _bus.WriteBytes(cmd);

            var dataBytes = ToLittleEndianBytes(data);
            _bus.WriteBytes(dataBytes);
        }

        internal void HaltMcu()
        {
            var cmd = new byte[] { 0xA1, 0x2C, 0x56, 0x78 };
            _bus.WriteBytes(cmd);
            Thread.Sleep(50); // wait for system reset
        }

        internal void ResetMcu()
        {
            var cmd = new byte[] { 0xA1, 0x2C, 0xAB, 0xCD };
            _bus.WriteBytes(cmd);
            Thread.Sleep(50); // wait for system reset
        }

        private static byte[] BuildAddressCommand(uint address)
        {
            var cmd = new byte[8];

            cmd[0] = 0xA1;
            cmd[1] = 0x2C;
            cmd[2] = 0x12;
            cmd[3] = 0x34;
            cmd[4] = (byte)((address >> 24) & 0xFF);
            cmd[5] = (byte)((address >> 16) & 0xFF);
            cmd[6] = (byte)((address >> 8) & 0xFF);
            cmd[7] = (byte)(address & 0xFF);

            return cmd;
        }

        private static byte[] ToLittleEndianBytes(uint value)
        {
            var data = new byte[4];

            data[0] = (byte)(value & 0xFF);
            data[1] = (byte)((value >> 8) & 0xFF);
            data[2] = (byte)((value >> 16) & 0xFF);
            data[3] = (byte)((value >> 24) & 0xFF);

            return data;
        }
    }

    internal class OasisTestSuite : IChipTestSuite
    {
        private readonly OasisRegisterChip _chip;
        private string _firmwareFilePath = string.Empty;
        private int _flashDumpSize = 0; // 0이면 fw 파일 크기 기준으로 덤프

        private enum TEST_ITEMS
        {
            GPIO_DISABLE,
            GPIO_04_ABGR,
            GPIO_04_RETLDO,
            GPIO_04_MBGR,
            GPIO_04_DALDO,
            GPIO_16_32KOSC,
            NUM_TEST_ITEMS,
        }

        private enum AUTO_TEST_ITEMS
        {
            NUM_TEST_ITEMS,
        }

        private enum FW_DN_ITEMS
        {
            FLASH_ERASE,
            FLASH_WRITE,
            FLASH_READ,
            FLASH_VERIFY,
            FIRM_ON_CLEAR,
            RAM_WRITE,
            RAM_READ,
            RESET,
            NUM_TEST_ITEMS,
        }

        private enum CAL_ITEMS
        {
            TEST_ITEM,
            NUM_TEST_ITEMS,
        }

        private enum FLASH_CMD : byte
        {
            WRSR = 0x01,    // write status reg
            PP = 0x02,      // page program
            RDCMD = 0x03,   // read data
            WRDI = 0x04,    // write disable
            RDSR = 0x05,    // read status reg
            WREN = 0x06,    // write enable
            F_RD = 0x0B,    // fast read
            SE = 0x20,      // 4KB sector erase
            BE32 = 0x52,    // 32KB block erase
            RSTEN = 0x66,   // reset enable
            REMS = 0x90,    // read manufacture
            RST = 0x99,     // reset
            RDID = 0x9F,    // read identification
            RES = 0xAB,     // read signature
            ENSO = 0xB1,    // enter secured OTP
            DP = 0xB9,      // deep power down
            EXSO = 0xC1,    // exit secured OTP
            CE = 0xC7,      // chip(bulk) erase
            BE64 = 0xD8,    // 64KB sector erase
        }

        private enum FW_TARGET
        {
            NV_MEM = 0,
            RAM = 1,
            SPI,
        }

        public IReadOnlyList<ChipTestInfo> Tests { get; }

        public OasisTestSuite(OasisRegisterChip chip)
        {
            _chip = chip ?? throw new ArgumentNullException(nameof(chip));

            Tests = new[]
            {
                new ChipTestInfo("TEST.GPIO_DISABLE",   "GPIO Disable",      "GPIO Disable",        "TEST"),
                new ChipTestInfo("TEST.GPIO_04_ABGR",   "GPIO4 ABGR",        "GPIO4 ABGR 출력",     "TEST"),
                new ChipTestInfo("TEST.GPIO_04_RETLDO", "GPIO4 RETLDO",      "GPIO4 RETLDO 출력",   "TEST"),
                new ChipTestInfo("TEST.GPIO_04_MBGR",   "GPIO4 MBGR",        "GPIO4 MBGR 출력",     "TEST"),
                new ChipTestInfo("TEST.GPIO_04_DALDO",  "GPIO4 DALDO",       "GPIO4 DALDO 출력",    "TEST"),
                new ChipTestInfo("TEST.GPIO_16_32KOSC", "GPIO16 32K OSC",    "GPIO16 32k OSC 출력", "TEST"),

                new ChipTestInfo("FW.FLASH_ERASE",      "Flash Erase",       "NV Flash Erase",      "FW"),
                new ChipTestInfo("FW.FLASH_WRITE",      "Flash Write",       "NV Flash Download",   "FW"),
                new ChipTestInfo("FW.FLASH_READ",       "Flash Read",        "NV Flash Dump",       "FW"),
                new ChipTestInfo("FW.FLASH_VERIFY",     "Flash Verify",      "Flash Verify",        "FW"),
                new ChipTestInfo("FW.FIRM_ON_CLEAR",    "FirmOn Clear",      "0x0F Command",        "FW"),
                new ChipTestInfo("FW.RAM_WRITE",        "RAM Write",         "RAM Download",        "FW"),
                new ChipTestInfo("FW.RAM_READ",         "RAM Read",          "RAM Dump",            "FW"),
                new ChipTestInfo("FW.RESET",            "Reset Oasis",       "Chip Reset",          "FW"),
            };
        }

        public void SetFirmwareFilePath(string path)
        {
            _firmwareFilePath = path;
        }

        public void SetFlashDumpSize(int size) => _flashDumpSize = size;

        public async Task RunTestAsync(
            string testId,
            Func<string, string, Task> log,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(testId))
            {
                await log("ERROR", "TestId is empty.");
                return;
            }

            await log("INFO", $"Oasis Test '{testId}' 시작");

            try
            {
                var parts = testId.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    await log("ERROR", $"Invalid test id format: {testId}");
                    return;
                }

                string category = parts[0];
                string itemName = parts[1];

                switch (category)
                {
                    case "TEST":
                        await RunTestCategoryAsync(itemName, log, cancellationToken);
                        break;
                    case "AUTO":
                        await RunAutoCategoryAsync(itemName, log, cancellationToken);
                        break;
                    case "CAL":
                        await RunCalCategoryAsync(itemName, log, cancellationToken);
                        break;
                    case "FW":
                        await RunFwCategoryAsync(itemName, log, cancellationToken);
                        break;
                    default:
                        await log("ERROR", $"Unknown category: {category}");
                        break;
                }
            }
            finally
            {
                await log("INFO", $"Oasis Test '{testId}' 종료");
            }
        }

        private async Task RunTestCategoryAsync(string itemName, Func<string, string, Task> log, CancellationToken ct)
        {
            if (!Enum.TryParse(itemName, out TEST_ITEMS item))
            {
                await log("ERROR", $"Unknown TEST item: {itemName}");
                return;
            }

            switch (item)
            {
                case TEST_ITEMS.GPIO_DISABLE:
                    await SetGpioDisableAsync(log, ct);
                    break;
                case TEST_ITEMS.GPIO_04_ABGR:
                    await SetGpio4AbgrAsync(true, log, ct);
                    break;
                case TEST_ITEMS.GPIO_04_RETLDO:
                    await SetGpio4RetLdoAsync(true, log, ct);
                    break;
                case TEST_ITEMS.GPIO_04_MBGR:
                    await SetGpio4MbgrAsync(true, log, ct);
                    break;
                case TEST_ITEMS.GPIO_04_DALDO:
                    await SetGpio4DaldoAsync(true, log, ct);
                    break;
                case TEST_ITEMS.GPIO_16_32KOSC:
                    await SetGpio16_32KoscAsync(true, log, ct);
                    break;
                default:
                    await log("ERROR", $"Unhandled TEST item: {itemName}");
                    break;
            }
        }

        private async Task RunAutoCategoryAsync(string itemName, Func<string, string, Task> log, CancellationToken ct)
        {
            if (!Enum.TryParse(itemName, out AUTO_TEST_ITEMS item))
            {
                await log("ERROR", $"Unknown AUTO item: {itemName}");
                return;
            }

            await log("INFO", $"AUTO test '{item}' is not implemented yet.");
            await Task.Delay(100, ct);
        }

        private async Task RunCalCategoryAsync(string itemName, Func<string, string, Task> log, CancellationToken ct)
        {
            if (!Enum.TryParse(itemName, out CAL_ITEMS item))
            {
                await log("ERROR", $"Unknown CAL item: {itemName}");
                return;
            }

            await log("INFO", $"CAL test '{item}' is not implemented yet.");
            await Task.Delay(100, ct);
        }

        private async Task RunFwCategoryAsync(string itemName, Func<string, string, Task> log, CancellationToken ct)
        {
            if (!Enum.TryParse(itemName, out FW_DN_ITEMS item))
            {
                await log("ERROR", $"Unknown FW item: {itemName}");
                return;
            }

            switch (item)
            {
                case FW_DN_ITEMS.FLASH_ERASE:
                    await RunFlashEraseAsync(log, ct);
                    break;
                case FW_DN_ITEMS.FLASH_WRITE:
                    await RunFlashWriteAsync(log, ct);
                    break;
                case FW_DN_ITEMS.FLASH_READ:
                    await RunFlashReadAsync(log, ct);
                    break;
                case FW_DN_ITEMS.FLASH_VERIFY:
                    await RunFlashVerifyAsync(log, ct);
                    break;
                case FW_DN_ITEMS.FIRM_ON_CLEAR:
                    await RunFirmOnClearAsync(log, ct);
                    break;
                case FW_DN_ITEMS.RAM_WRITE:
                    await RunRamWriteAsync(log, ct);
                    break;
                case FW_DN_ITEMS.RAM_READ:
                    await RunRamReadAsync(log, ct);
                    break;
                case FW_DN_ITEMS.RESET:
                    await RunResetAsync(log, ct);
                    break;
                default:
                    await log("INFO", $"FW item '{item}' is not implemented yet.");
                    break;
            }
        }

        private async Task RunFlashEraseAsync(Func<string, string, Task> log, CancellationToken ct)
        {
            if (!await CheckI2cIdAsync(log, ct))
                return;

            await log("INFO", "Start FLASH_ERASE (8 sectors of 64KB).");

            for (uint num = 0; num < 8; num++)
            {
                ct.ThrowIfCancellationRequested();

                uint secAddr = num * 0x10000u;
                await log("INFO", $"Erase sector #{num} @ 0x{secAddr:X8}");

                uint cmd = ((uint)FLASH_CMD.BE64 << 24) | (secAddr & 0x00FFFFFFu);
                _chip.WriteRegister(0x5009_0008, cmd);

                bool ok = await WaitFlashReadyAsync(log, ct);
                if (!ok)
                {
                    await log("ERROR", $"Sector erase timeout @ 0x{secAddr:X8}");
                    return;
                }

                await log("INFO", $"Sector #{num} erase OK.");
            }

            await log("INFO", "FLASH_ERASE completed successfully.");
        }

        private async Task SetGpioDisableAsync(Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                await SetGpio4AbgrAsync(false, log, ct);
                await SetGpio16_32KoscAsync(false, log, ct);
                await SetGpio4RetLdoAsync(false, log, ct);
                await SetGpio4MbgrAsync(false, log, ct);
                await SetGpio4DaldoAsync(false, log, ct);
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Error in SetGpioDisableAsync: {ex.Message}");
                throw;
            }
        }

        private async Task SetGpio4AbgrAsync(bool enable, Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                uint regDC340050 = _chip.ReadRegister(0xDC34_0050);
                uint regDC340054 = _chip.ReadRegister(0xDC34_0054);
                uint regDC34006C = _chip.ReadRegister(0xDC34_006C);

                if (enable)
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 | (1u << 15));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 | 15u);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C | (1u << 15));
                    await Task.Delay(10, ct);
                }
                else
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 & 0x7FFFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 & 0xFFF0u);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C & 0x7FFFu);
                    await Task.Delay(10, ct);
                }
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Error in SetGpio4AbgrAsync: {ex.Message}");
                throw;
            }
        }

        private async Task SetGpio16_32KoscAsync(bool enable, Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                uint regDC340050 = _chip.ReadRegister(0xDC34_0050);
                uint regDC340054 = _chip.ReadRegister(0xDC34_0054);
                uint regDC34006C = _chip.ReadRegister(0xDC34_006C);

                if (enable)
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 | (1u << 14));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 | (1u << 8));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C | (1u << 14));
                    await Task.Delay(10, ct);
                }
                else
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 & 0xBFFFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 & 0xFEFFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C & 0xBFFFu);
                    await Task.Delay(10, ct);
                }
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Error in SetGpio16_32KoscAsync: {ex.Message}");
                throw;
            }
        }

        private async Task SetGpio4RetLdoAsync(bool enable, Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                uint regDC340050 = _chip.ReadRegister(0xDC34_0050);
                uint regDC340054 = _chip.ReadRegister(0xDC34_0054);
                uint regDC34006C = _chip.ReadRegister(0xDC34_006C);

                if (enable)
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 | (1u << 13));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 | (0xFu << 4));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C | (1u << 13));
                    await Task.Delay(10, ct);
                }
                else
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 & 0xDFFFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 & 0xFF0Fu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C & 0xDFFFu);
                    await Task.Delay(10, ct);
                }
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Error in SetGpio4RetLdoAsync: {ex.Message}");
                throw;
            }
        }

        private async Task SetGpio4MbgrAsync(bool enable, Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                uint regDC340050 = _chip.ReadRegister(0xDC34_0050);
                uint regDC340054 = _chip.ReadRegister(0xDC34_0054);
                uint regDC34006C = _chip.ReadRegister(0xDC34_006C);

                if (enable)
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 | (1u << 12));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 | (0xFu << 12));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C | (1u << 12));
                    await Task.Delay(10, ct);
                }
                else
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 & 0xEFFFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 & 0xF0FFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C & 0xEFFFu);
                    await Task.Delay(10, ct);
                }
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Error in SetGpio4MbgrAsync: {ex.Message}");
                throw;
            }
        }

        private async Task SetGpio4DaldoAsync(bool enable, Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                uint regDC340050 = _chip.ReadRegister(0xDC34_0050);
                uint regDC340054 = _chip.ReadRegister(0xDC34_0054);
                uint regDC34006C = _chip.ReadRegister(0xDC34_006C);

                if (enable)
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 | (1u << 11));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 | (0xFu << 20));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C | (1u << 11));
                    await Task.Delay(10, ct);
                }
                else
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 & 0xF7FFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 & 0x0FFFFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C & 0xF7FFu);
                    await Task.Delay(10, ct);
                }
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Error in SetGpio4DaldoAsync: {ex.Message}");
                throw;
            }
        }

        private async Task RunFlashWriteAsync(Func<string, string, Task> log, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_firmwareFilePath))
            {
                await log("ERROR", "Firmware file path is not set.");
                return;
            }

            byte[] fwData;
            try
            {
                fwData = File.ReadAllBytes(_firmwareFilePath);
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Failed to read firmware file: {ex.Message}");
                return;
            }

            if (fwData.Length == 0)
            {
                await log("ERROR", "Firmware file is empty.");
                return;
            }

            if (!await CheckI2cIdAsync(log, ct))
                return;

            const int PageSize = 256;
            byte[] pageBuffer = new byte[PageSize];

            for (uint flashAddress = 0; flashAddress < fwData.Length; flashAddress += (uint)PageSize)
            {
                ct.ThrowIfCancellationRequested();

                for (int i = 0; i < PageSize; i++)
                {
                    int srcIndex = (int)flashAddress + i;
                    pageBuffer[i] = srcIndex < fwData.Length ? fwData[srcIndex] : (byte)0xFF;
                }

                await log("INFO", $"Write page @ 0x{flashAddress:X8}");

                if (!await WriteMemoryNvmAsync(flashAddress, pageBuffer, log, ct))
                    return;
            }

            await log("INFO", "Verify written data");

            byte[] readBuffer = new byte[PageSize];
            for (uint flashAddress = 0; flashAddress < fwData.Length; flashAddress += (uint)PageSize)
            {
                ct.ThrowIfCancellationRequested();

                for (uint i = 0; i < PageSize; i += 4)
                {
                    uint data = _chip.ReadRegister(flashAddress + i);
                    for (int j = 0; j < 4; j++)
                    {
                        int idx = (int)i + j;
                        if (idx < PageSize)
                            readBuffer[idx] = (byte)((data >> (8 * j)) & 0xFF);
                    }
                }

                for (int i = 0; i < PageSize; i++)
                {
                    byte expected = ((int)flashAddress + i < fwData.Length) ? fwData[(int)flashAddress + i] : (byte)0xFF;
                    if (readBuffer[i] != expected)
                    {
                        await log("ERROR", $"Verify failed @ 0x{flashAddress + (uint)i:X8}: W=0x{expected:X2}, R=0x{readBuffer[i]:X2}");
                        return;
                    }
                }
            }

            await log("INFO", "FLASH_WRITE completed successfully.");
        }

        private async Task RunFlashReadAsync(Func<string, string, Task> log, CancellationToken ct)
        {
            await log("INFO", "Start FLASH_READ (Dump NV memory).");

            if (!await CheckI2cIdAsync(log, ct))
                return;

            int dumpSize = _flashDumpSize;
            if (dumpSize <= 0)
            {
                if (!string.IsNullOrWhiteSpace(_firmwareFilePath) && File.Exists(_firmwareFilePath))
                {
                    dumpSize = (int)new FileInfo(_firmwareFilePath).Length;
                }
                else
                {
                    dumpSize = 256 * 1024;
                    await log("INFO", $"Dump size is not set. Use default {dumpSize} bytes (256KB).");
                }
            }

            const int PageSize = 4;
            var firmwareData = new List<byte>(dumpSize);

            for (uint addr = 0; addr < dumpSize; addr += PageSize)
            {
                ct.ThrowIfCancellationRequested();

                if ((addr % 0x1000) == 0)
                    await log("INFO", $"Read Flash @ 0x{addr:X8}");

                uint rcv = _chip.ReadRegister(addr);

                firmwareData.Add((byte)(rcv & 0xFF));
                firmwareData.Add((byte)((rcv >> 8) & 0xFF));
                firmwareData.Add((byte)((rcv >> 16) & 0xFF));
                firmwareData.Add((byte)((rcv >> 24) & 0xFF));
            }

            string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"ReadFW_NVM_{time}.bin";
            try
            {
                File.WriteAllBytes(fileName, firmwareData.ToArray());
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Failed to write dump file '{fileName}': {ex.Message}");
                return;
            }

            await log("INFO", $"FLASH_READ completed. File = {fileName}, Size = {firmwareData.Count} bytes.");
        }

        private async Task RunFlashVerifyAsync(Func<string, string, Task> log, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_firmwareFilePath) || !File.Exists(_firmwareFilePath))
            {
                await log("ERROR", "Firmware file path is not set or file does not exist. Cannot verify.");
                return;
            }

            byte[] fwData;
            try
            {
                fwData = File.ReadAllBytes(_firmwareFilePath);
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Failed to read firmware file: {ex.Message}");
                return;
            }

            if (fwData.Length == 0)
            {
                await log("ERROR", "Firmware file is empty. Cannot verify.");
                return;
            }

            if (!await CheckI2cIdAsync(log, ct))
                return;

            await log("INFO", $"Start FLASH_VERIFY. Size = {fwData.Length} bytes");

            const int PageSize = 256;
            byte[] readBuffer = new byte[PageSize];

            for (uint flashAddress = 0; flashAddress < fwData.Length; flashAddress += (uint)PageSize)
            {
                ct.ThrowIfCancellationRequested();

                if ((flashAddress % 0x1000) == 0)
                    await log("INFO", $"Verify @ 0x{flashAddress:X8}");

                for (uint i = 0; i < PageSize; i += 4)
                {
                    uint data = _chip.ReadRegister(flashAddress + i);
                    for (int j = 0; j < 4; j++)
                    {
                        int idx = (int)i + j;
                        if (idx < PageSize)
                            readBuffer[idx] = (byte)((data >> (8 * j)) & 0xFF);
                    }
                }

                for (int i = 0; i < PageSize; i++)
                {
                    int srcIndex = (int)flashAddress + i;
                    byte expected = srcIndex < fwData.Length ? fwData[srcIndex] : (byte)0xFF;

                    if (readBuffer[i] != expected)
                    {
                        uint errAddr = flashAddress + (uint)i;
                        await log("ERROR",
                            $"Verify failed @ 0x{errAddr:X8}: W=0x{expected:X2}, R=0x{readBuffer[i]:X2}");
                        return;
                    }
                }
            }

            await log("INFO", "FLASH_VERIFY completed successfully.");
        }

        private async Task RunResetAsync(Func<string, string, Task> log, CancellationToken ct)
        {
            await log("INFO", "Reset Oasis (MCU reset).");

            if (!await CheckI2cIdAsync(log, ct))
                return;

            _chip.ResetMcu();

            await Task.Delay(100, ct);

            await log("INFO", "Reset command has been sent.");
        }

        private async Task RunFirmOnClearAsync(Func<string, string, Task> log, CancellationToken ct)
        {
            await log("INFO", "FIRM_ON_CLEAR is not implemented in V4 yet.");
            await Task.Delay(100, ct);
        }

        private async Task RunRamWriteAsync(Func<string, string, Task> log, CancellationToken ct)
        {
            await log("INFO", "RAM_WRITE is not implemented in V4 yet.");
            await Task.Delay(100, ct);
        }

        private async Task RunRamReadAsync(Func<string, string, Task> log, CancellationToken ct)
        {
            await log("INFO", "RAM_READ is not implemented in V4 yet.");
            await Task.Delay(100, ct);
        }

        private async Task<bool> CheckI2cIdAsync(
            Func<string, string, Task> log,
            CancellationToken ct)
        {
            // 단순히 동기 ReadRegister를 호출하고 결과만 검사한다.
            uint id = _chip.ReadRegister(0x50000000);
            uint ipId = id >> 12;

            if (ipId != 0x02021)
            {
                await log("ERROR", $"Fail to Check I2C IP ID. R = 0x{ipId:X5}");
                return false;
            }

            await log("INFO", "CheckI2C_ID OK.");
            return true;
        }

        private async Task<bool> WaitFlashReadyAsync(Func<string, string, Task> log, CancellationToken ct, int maxLoopCount = 20, int delayMs = 200)
        {
            for (int cnt = 0; cnt < maxLoopCount; cnt++)
            {
                ct.ThrowIfCancellationRequested();

                uint status = _chip.ReadRegister(0x5009_0020);
                uint busy = status & 0x01u;

                if (busy == 0)
                    return true;

                await Task.Delay(delayMs, ct);
            }

            await log("ERROR", "Flash controller did not become ready within timeout.");
            return false;
        }

        private async Task<bool> WriteMemoryNvmAsync(uint flashAddress, byte[] pageBuffer, Func<string, string, Task> log, CancellationToken ct)
        {
            const uint FlashTxBase = 0x5009_1000;
            const uint FlashCmdReg = 0x5009_0008;
            const uint FlashStatusReg = 0x5009_000C;
            const byte FlashCmdPageProgram = (byte)FLASH_CMD.PP;

            for (int i = 0; i < pageBuffer.Length; i += 4)
            {
                ct.ThrowIfCancellationRequested();

                uint word = (uint)(pageBuffer[i]
                    | (pageBuffer[Math.Min(i + 1, pageBuffer.Length - 1)] << 8)
                    | (pageBuffer[Math.Min(i + 2, pageBuffer.Length - 1)] << 16)
                    | (pageBuffer[Math.Min(i + 3, pageBuffer.Length - 1)] << 24));

                _chip.WriteRegister(FlashTxBase + (uint)i, word);
            }

            _chip.WriteRegister(FlashCmdReg, (uint)((FlashCmdPageProgram << 24) | (flashAddress & 0xFFFFFFu)));

            for (int retry = 0; retry < 2000; retry++)
            {
                ct.ThrowIfCancellationRequested();

                uint status = _chip.ReadRegister(FlashStatusReg);
                if ((status & 0x1u) == 0)
                    return true;

                await Task.Delay(1, ct);
            }

            await log("ERROR", "Timeout waiting for flash page program to complete.");
            return false;
        }
    }
}
