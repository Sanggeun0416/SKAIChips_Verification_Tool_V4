using System;
using SKAIChips_Verification_Tool.Core;

namespace SKAIChips_Verification_Tool.Chips
{
    public class Oasis : IRegisterChip
    {
        private readonly IBus _bus;

        public string Name { get; }

        public Oasis(string name, IBus bus)
        {
            Name = name;
            _bus = bus;
        }

        public uint ReadRegister(uint address)
        {
            byte[] cmd = new byte[8];
            cmd[0] = 0xA1;
            cmd[1] = 0x2C;
            cmd[2] = 0x12;
            cmd[3] = 0x34;
            cmd[4] = (byte)((address >> 24) & 0xFF);
            cmd[5] = (byte)((address >> 16) & 0xFF);
            cmd[6] = (byte)((address >> 8) & 0xFF);
            cmd[7] = (byte)((address >> 0) & 0xFF);

            _bus.WriteBytes(cmd);

            byte[] rcv = _bus.ReadBytes(4);
            uint data = (uint)((rcv[3] << 24) | (rcv[2] << 16) | (rcv[1] << 8) | rcv[0]);

            return data;
        }

        public void WriteRegister(uint address, uint data)
        {
            byte[] cmd = new byte[8];
            cmd[0] = 0xA1;
            cmd[1] = 0x2C;
            cmd[2] = 0x12;
            cmd[3] = 0x34;
            cmd[4] = (byte)((address >> 24) & 0xFF);
            cmd[5] = (byte)((address >> 16) & 0xFF);
            cmd[6] = (byte)((address >> 8) & 0xFF);
            cmd[7] = (byte)((address >> 0) & 0xFF);

            _bus.WriteBytes(cmd);

            byte[] dataBytes = new byte[4];
            dataBytes[0] = (byte)((data >> 0) & 0xFF);
            dataBytes[1] = (byte)((data >> 8) & 0xFF);
            dataBytes[2] = (byte)((data >> 16) & 0xFF);
            dataBytes[3] = (byte)((data >> 24) & 0xFF);

            _bus.WriteBytes(dataBytes);
        }
    }

    public class OasisProject : IChipProject
    {
        public string Name => "Oasis";

        public ProtocolType[] SupportedProtocols => new[] { ProtocolType.I2C };

        public IRegisterChip CreateChip(IBus bus, ProtocolSettings settings)
        {
            if (settings.ProtocolType != ProtocolType.I2C)
                throw new InvalidOperationException("Oasis supports only I2C.");

            return new Oasis("Oasis", bus);
        }
    }
}
