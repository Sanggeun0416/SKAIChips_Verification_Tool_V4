using System;
using System.Threading;
using System.Threading.Tasks;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public class DummyAutoTask : IAutoTask
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name { get; }

        public DummyAutoTask(string name)
        {
            Name = name;
        }

        public async Task RunAsync(
            AutoTaskContext context,
            IProgress<AutoTaskProgress> progress,
            CancellationToken token)
        {
            int total = 10;

            for (int i = 0; i < total; i++)
            {
                token.ThrowIfCancellationRequested();

                progress.Report(new AutoTaskProgress
                {
                    State = AutoTaskState.Running,
                    CurrentStep = i + 1,
                    TotalSteps = total,
                    Message = $"Step {i + 1} / {total}"
                });

                await Task.Delay(400, token);
            }
        }
    }
}
