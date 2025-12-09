using System.Threading;
using System.Threading.Tasks;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public sealed class DelayBlock : AutoBlockBase
    {
        public int Milliseconds { get; set; }

        public DelayBlock() : base("Delay")
        {
            Milliseconds = 1000;
        }

        public override async Task ExecuteAsync(AutoTaskContext context, CancellationToken token)
        {
            if (Milliseconds <= 0)
                return;

            await Task.Delay(Milliseconds, token);
        }
    }
}
