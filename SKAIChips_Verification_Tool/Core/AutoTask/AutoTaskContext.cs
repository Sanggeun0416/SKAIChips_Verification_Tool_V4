using System.Collections.Generic;
using System.Threading;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public class AutoTaskContext
    {
        // 나중에 여기다가 Chip, InstrumentManager, Logger 넣을 예정
        public CancellationToken CancellationToken { get; }

        public Dictionary<string, object> Variables { get; } =
            new Dictionary<string, object>();

        public AutoTaskContext()
        {
        }

        public AutoTaskContext(CancellationToken token)
        {
            CancellationToken = token;
        }
    }
}
