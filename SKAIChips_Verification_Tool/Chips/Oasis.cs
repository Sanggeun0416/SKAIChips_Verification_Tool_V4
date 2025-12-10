using System;
using System.Collections.Generic;
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

        public IReadOnlyList<ChipTestInfo> Tests { get; }

        public OasisTestSuite(OasisRegisterChip chip)
        {
            _chip = chip;

            Tests = new[]
            {
                new ChipTestInfo("dummy", "Dummy Test", "Oasis 테스트 시퀀스 자리")
            };
        }

        public async Task RunTestAsync(
            string testId,
            Func<string, string, Task> log,
            CancellationToken cancellationToken)
        {
            await log("INFO", $"Oasis Test '{testId}' 시작");

            switch (testId)
            {
                case "dummy":
                    await log("INFO", "아직 구현 안 됨");
                    await Task.Delay(500, cancellationToken);
                    break;

                default:
                    await log("ERROR", $"Unknown testId: {testId}");
                    break;
            }

            await log("INFO", $"Oasis Test '{testId}' 종료");
        }
    }
}
