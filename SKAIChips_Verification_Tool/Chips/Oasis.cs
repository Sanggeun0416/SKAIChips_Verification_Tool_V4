using System;
using SKAIChips_Verification_Tool.Core;

namespace SKAIChips_Verification_Tool.Chips
{
    public sealed class Oasis : IRegisterChip
    {
        #region Fields

        private readonly IBus _bus;

        #endregion

        #region Properties

        public string Name { get; }

        #endregion

        #region Constructors

        public Oasis(string name, IBus bus)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        #endregion

        #region Private Methods

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

        #endregion

        #region Public Methods

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

        #endregion
    }

    public sealed class OasisProject : IChipProject
    {
        #region Properties

        public string Name => "Oasis";

        public ProtocolType[] SupportedProtocols { get; } = { ProtocolType.I2C };

        #endregion

        #region Methods

        public IRegisterChip CreateChip(IBus bus, ProtocolSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            if (settings.ProtocolType != ProtocolType.I2C)
                throw new InvalidOperationException("Oasis supports only I2C.");

            return new Oasis("Oasis", bus);
        }

        #endregion
    }
}
