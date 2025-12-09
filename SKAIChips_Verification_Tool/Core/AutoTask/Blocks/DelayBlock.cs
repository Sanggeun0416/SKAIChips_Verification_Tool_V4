using System.Threading;
using System.Threading.Tasks;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public sealed class DelayBlock : AutoBlockBase
    {
        #region Properties

        public int Milliseconds { get; set; }

        #endregion

        #region Constructors

        public DelayBlock() : base("Delay")
        {
            Milliseconds = 1000;
        }

        #endregion

        #region Methods

        public override async Task ExecuteAsync(AutoTaskContext context, CancellationToken token)
        {
            if (Milliseconds <= 0)
                return;

            await Task.Delay(Milliseconds, token);
        }

        #endregion
    }
}
