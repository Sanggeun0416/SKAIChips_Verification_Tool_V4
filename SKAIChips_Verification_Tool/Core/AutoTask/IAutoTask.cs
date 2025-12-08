using System.Threading;
using System.Threading.Tasks;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public interface IAutoTask
    {
        string Id { get; }
        string Name { get; }

        Task RunAsync(
            AutoTaskContext context,
            IProgress<AutoTaskProgress> progress,
            CancellationToken token);
    }
}
