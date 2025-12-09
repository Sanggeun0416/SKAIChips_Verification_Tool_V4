using System;
using System.Threading;
using System.Threading.Tasks;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public interface IAutoTask
    {
        string Name { get; }

        Task ExecuteAsync(
            AutoTaskContext context,
            IProgress<AutoTaskProgress> progress,
            CancellationToken cancellationToken);
    }
}
