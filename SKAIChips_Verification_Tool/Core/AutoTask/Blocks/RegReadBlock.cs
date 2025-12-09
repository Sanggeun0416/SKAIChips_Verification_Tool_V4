using System;
using System.Threading;
using System.Threading.Tasks;
using SKAIChips_Verification_Tool.Chips;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public sealed class RegReadBlock : AutoBlockBase
    {
        #region Properties

        public uint Address { get; }

        public string ResultKey { get; set; }

        #endregion

        #region Constructors

        public RegReadBlock(uint address)
            : base($"READ 0x{address:X8}")
        {
            Address = address;
            ResultKey = null;
        }

        #endregion

        #region Methods

        public override async Task ExecuteAsync(AutoTaskContext context, CancellationToken token)
        {
            var chip = context.Get<IRegisterChip>("Chip")
                       ?? throw new InvalidOperationException("AutoTaskContext에 'Chip'이 설정되어 있지 않습니다.");

            var value = await Task.Run(() => chip.ReadRegister(Address), token);

            context.RegisterUpdatedCallback?.Invoke(Address, value);

            context.LogCallback?.Invoke(
                "READ",
                $"0x{Address:X8}",
                $"0x{value:X8}",
                "OK");

            if (!string.IsNullOrEmpty(ResultKey))
            {
                context.Variables[ResultKey] = value;
            }
        }

        #endregion
    }
}
