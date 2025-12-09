using System;
using System.Threading;
using System.Threading.Tasks;
using SKAIChips_Verification_Tool.Chips;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public sealed class RegReadBlock : AutoBlockBase
    {
        public uint Address { get; }

        // 읽은 값을 Context.Variables에 저장하고 싶을 때 사용할 키(옵션)
        public string ResultKey { get; set; }

        public RegReadBlock(uint address)
            : base($"READ 0x{address:X8}")
        {
            Address = address;
            ResultKey = null;   // 필요하면 Editor에서 설정
        }

        public override async Task ExecuteAsync(AutoTaskContext context, CancellationToken token)
        {
            var chip = context.Get<IRegisterChip>("Chip");
            if (chip == null)
                throw new InvalidOperationException("AutoTaskContext에 'Chip'이 설정되어 있지 않습니다.");

            uint value = await Task.Run(() => chip.ReadRegister(Address), token);

            // UI 쪽 레지스터 값 업데이트
            context.RegisterUpdatedCallback?.Invoke(Address, value);

            // Register Control Log 콜백
            context.LogCallback?.Invoke(
                "READ",
                $"0x{Address:X8}",
                $"0x{value:X8}",
                "OK");

            // 필요하면 변수로 저장
            if (!string.IsNullOrEmpty(ResultKey))
            {
                context.Variables[ResultKey] = value;
            }
        }
    }
}
