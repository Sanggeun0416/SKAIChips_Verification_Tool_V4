using System;
using System.Threading;
using System.Threading.Tasks;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public class DummyAutoTask : IAutoTask
    {
        public string Name { get; }

        public DummyAutoTask(string name = "DummyTask")
        {
            Name = name;
        }

        public async Task ExecuteAsync(
            AutoTaskContext context,
            IProgress<AutoTaskProgress> progress,
            CancellationToken token)
        {
            int total = 10;

            for (int i = 0; i < total; i++)
            {
                token.ThrowIfCancellationRequested();

                // 읽기 전용 프로퍼티 구조에 맞게, 매번 새 AutoTaskProgress 인스턴스 만들어서 Report
                progress?.Report(new AutoTaskProgress(
                    AutoTaskState.Running,
                    i + 1,
                    total,
                    $"Dummy step {i + 1}/{total}"));

                await Task.Delay(200, token);
            }

            // 끝났다는 의미로 한 번 더 보내고 싶으면 옵션
            progress?.Report(new AutoTaskProgress(
                AutoTaskState.Completed,
                total,
                total,
                "Dummy task completed"));
        }
    }
}
