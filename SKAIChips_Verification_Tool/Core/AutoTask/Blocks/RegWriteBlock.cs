using System;
using System.Threading;
using System.Threading.Tasks;
using SKAIChips_Verification_Tool.Chips;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public sealed class RegWriteBlock : AutoBlockBase
    {
        public uint Address { get; }
        public uint Value { get; }

        public RegWriteBlock(uint address, uint value)
            : base($"WRITE 0x{address:X8} = 0x{value:X8}")
        {
            Address = address;
            Value = value;
        }

        public override async Task ExecuteAsync(AutoTaskContext context, CancellationToken token)
        {
            var chip = context.Get<IRegisterChip>("Chip");
            if (chip == null)
                throw new InvalidOperationException("AutoTaskContext에 'Chip'이 설정되어 있지 않습니다.");

            await Task.Run(() => chip.WriteRegister(Address, Value), token);

            // 레지스터 값 변경 콜백
            context.RegisterUpdatedCallback?.Invoke(Address, Value);

            // Register Control Log 콜백
            context.LogCallback?.Invoke(
                "WRITE",
                $"0x{Address:X8}",
                $"0x{Value:X8}",
                "OK");
        }
    }
}
